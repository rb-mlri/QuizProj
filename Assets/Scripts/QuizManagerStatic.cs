using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Collections;

public class QuizManagerStatic : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI questionText;
    public Button[] optionButtons; // size = 4
    public TextMeshProUGUI feedbackText;
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;
    public Button backToMenuButton;

    [Header("File Settings")]
    public TextAsset questionFile; // drag the text file that contains the questions
    public int totalQuestionsInTest = 20;

    private List<Question> allQuestions = new List<Question>();
    private List<Question> quizQuestions = new List<Question>();
    private int currentIndex = 0;
    private int score = 0;

    [Header("Quiz Settings")]
    public int easyQuestionsCount = 7;
    public int mediumQuestionsCount = 7;
    public int hardQuestionsCount = 6;

    [Header("Character")]
    [SerializeField] private Animator knightAnimator;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip slashClip;
    [SerializeField] private Scarecrow scarecrow;

    [Header("Timing Settings")]
    public float nextQuestionDelay = 2f;

    //CSV Generator
    [System.Serializable]
    public class QuestionResponse
    {
        public string questionText;
        public string selectedAnswer;
        public string correctAnswer;
        public bool isCorrect;
    }

    private List<QuestionResponse> responses = new List<QuestionResponse>();
    private List<int> userAnswers = new List<int>();

    void Start()
    {
        resultPanel.SetActive(false);

        if (questionFile == null)
        {
            Debug.LogError("No question file assigned in Inspector!");
            return;
        }

        allQuestions = LoadQuestionsFromFile();
        SelectBalancedQuestions();
        ShowQuestion();

        backToMenuButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("MainMenu");
        });
    }

    //------------------------- File Loader (Ex. questions'#'.txt) -------------------------//
    private List<Question> LoadQuestionsFromFile()
    {
        List<Question> questions = new List<Question>();
        string[] lines = questionFile.text.Split(new[] { '\n' }, System.StringSplitOptions.None);

        Question currentQuestion = null;
        List<string> questionTextBuffer = new List<string>();

        foreach (string rawLine in lines)
        {
            string line = rawLine.TrimEnd('\r');

            if (line.StartsWith("Level:"))
            {
                if (currentQuestion != null)
                {
                    currentQuestion.questionText = string.Join("\n", questionTextBuffer);
                    questions.Add(currentQuestion);
                }

                currentQuestion = new Question();
                questionTextBuffer.Clear();

                int level = int.Parse(line.Split(':')[1].Trim());
                currentQuestion.difficulty = (Difficulty)(level - 1);
            }
            else if (line.StartsWith("Q:"))
            {
                questionTextBuffer.Add(line.Substring(2).Trim());
            }
            else if (line.StartsWith("A)"))
            {
                currentQuestion.options = new string[4];
                currentQuestion.options[0] = line.Substring(2).Trim();
            }
            else if (line.StartsWith("B)"))
            {
                currentQuestion.options[1] = line.Substring(2).Trim();
            }
            else if (line.StartsWith("C)"))
            {
                currentQuestion.options[2] = line.Substring(2).Trim();
            }
            else if (line.StartsWith("D)"))
            {
                currentQuestion.options[3] = line.Substring(2).Trim();
            }
            else if (line.StartsWith("Answer:"))
            {
                currentQuestion.correctIndex = int.Parse(line.Split(':')[1].Trim());
                currentQuestion.questionText = string.Join("\n", questionTextBuffer);

                questions.Add(currentQuestion);
                currentQuestion = null;
                questionTextBuffer.Clear();
            }
            else
            {
                if (currentQuestion != null)
                    questionTextBuffer.Add(line);
            }
        }

        return questions;
    }



    //------------------------- Select Balanced Questions & Shuffle Each Question By Group Difficulty -------------------------//
    void SelectBalancedQuestions()
    {
        // Split questions by difficulty
        var easy = allQuestions.Where(q => q.questionText.Contains("(Easy)")).ToList();
        var medium = allQuestions.Where(q => q.questionText.Contains("(Medium)")).ToList();
        var hard = allQuestions.Where(q => q.questionText.Contains("(Hard)")).ToList();

        Shuffle(easy);
        Shuffle(medium);
        Shuffle(hard);

        quizQuestions.Clear();
        quizQuestions.AddRange(easy.Take(easyQuestionsCount));
        quizQuestions.AddRange(medium.Take(mediumQuestionsCount));
        quizQuestions.AddRange(hard.Take(hardQuestionsCount));
    }

    //------------------------- Randomizer -------------------------//
    void Shuffle(List<Question> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            var value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    //------------------------- Display Questions -------------------------//
    void ShowQuestion()
    {
        if (currentIndex >= quizQuestions.Count)
        {
            FinishQuiz();
            return;
        }

        Question q = quizQuestions[currentIndex];
        questionText.text = q.questionText;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            int choiceIndex = i;
            optionButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = q.options[i];
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => OnOptionSelected(choiceIndex));
        }

        if (feedbackText) feedbackText.text = "";
    }

    //------------------------- Chosen Answer Checker (Ex. If Right/Wrong) -------------------------//
    void OnOptionSelected(int choiceIndex)
    {
        Question q = quizQuestions[currentIndex];
        bool correct = choiceIndex == q.correctIndex;

        // Disable Buttons Once There's An Answer
        foreach (Button b in optionButtons)
            b.interactable = false;

        // Record response
        responses.Add(new QuestionResponse
        {
            questionText = q.questionText,
            selectedAnswer = q.options[choiceIndex],
            correctAnswer = q.options[q.correctIndex],
            isCorrect = correct
        });

        if (correct)
        {
            score++;
            if (feedbackText) feedbackText.text = "Correct!";

            if (knightAnimator != null)
            {
                knightAnimator.SetTrigger("Attack");
                sfxSource.PlayOneShot(slashClip);
                scarecrow.Wiggle();
            }
        }
        else
        {
            if (feedbackText) feedbackText.text = $"Wrong! Correct: {q.options[q.correctIndex]}";
        }

        currentIndex++;
        userAnswers.Add(choiceIndex);

        // Start coroutine to delay before next question
        StartCoroutine(NextQuestionAfterDelay());
    }

    // Delays Next Question
    IEnumerator NextQuestionAfterDelay()
    {
        yield return new WaitForSeconds(nextQuestionDelay);

        // Re-enable buttons for the next question
        foreach (Button b in optionButtons)
            b.interactable = true;

        ShowQuestion();
    }


    //------------------------- Get Results and Save for CSV (QuizProj/Assets/QuizResults) -------------------------//
    void SaveResultsToCSV()
    {
        int selectedLevel = PlayerPrefs.GetInt("SelectedLevel", 1);
        string fileName = $"QuizResults_Level{selectedLevel}_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv";
        string filePath = Path.Combine(Application.persistentDataPath, fileName);

        List<string> lines = new List<string>();
        lines.Add("Level,Question,Selected Answer,Correct Answer,Correct?");

        foreach (var r in responses)
        {
            // Escape commas in the question text
            string qText = r.questionText.Replace(",", ";");
            string selected = r.selectedAnswer.Replace(",", ";");
            string correct = r.correctAnswer.Replace(",", ";");
            lines.Add($"{selectedLevel},{qText},{selected},{correct},{r.isCorrect}");
        }

        // Add total score at the end
        lines.Add($",,,Total Score,{score}/{quizQuestions.Count}");

        File.WriteAllLines(filePath, lines.ToArray());

        Debug.Log($"CSV saved: {filePath}");
    }

    //------------------------- Finish Quiz -------------------------//
    void FinishQuiz()
    {
        questionText.gameObject.SetActive(false);
        feedbackText.gameObject.SetActive(false);
        foreach (Button b in optionButtons) b.gameObject.SetActive(false);

        resultPanel.SetActive(true);
        resultText.text = $"Score: {score}/{quizQuestions.Count}";

        GenerateCSV();
    }

    //------------------------- Get Saved Results And Put In CSV -------------------------//
    void GenerateCSV()
    {
        string mode = "Static"; // mode name
        int level = PlayerPrefs.GetInt("SelectedLevel", 1);

        // Create folder inside game directory if not exists
        string folderPath = Path.Combine(Application.dataPath, "QuizResults");
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        // CSV filename includes mode, level, timestamp
        string filePath = Path.Combine(folderPath, $"{mode}_QuizResults_Level{level}_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv");

        // CSV header without Difficulty
        string csvContent = "Question,SelectedAnswer,CorrectAnswer,Correct\n";

        for (int i = 0; i < quizQuestions.Count; i++)
        {
            Question q = quizQuestions[i];
            int userAnswer = i < userAnswers.Count ? userAnswers[i] : -1;
            bool correct = userAnswer == q.correctIndex;

            string questionTextClean = q.questionText.Replace("\n", " ").Replace(",", " ");
            string selectedAnswerText = userAnswer >= 0 ? q.options[userAnswer].Replace(",", " ") : "";
            string correctAnswerText = q.options[q.correctIndex].Replace(",", " ");

            csvContent += $"{questionTextClean},{selectedAnswerText},{correctAnswerText},{correct}\n";
        }

        // Prefix score with a single quote to force Excel to treat as text
        csvContent += $"\nTotal Score,'{score}/{quizQuestions.Count}'";

        File.WriteAllText(filePath, csvContent);
        Debug.Log("CSV saved at: " + filePath);
    }



    //------------------------- Main Menu -------------------------//
    public void BackToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
