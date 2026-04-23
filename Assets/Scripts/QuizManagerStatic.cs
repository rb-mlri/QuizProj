using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.SceneManagement;

public class QuizManagerStatic : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI questionText;
    public Button[] optionButtons;
    public TextMeshProUGUI feedbackText;
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;
    public Button backToMenuButton;
    public GameObject explanationPanel;
    public TextMeshProUGUI explanationText;
    public TextMeshProUGUI questionCounterText;

    [Header("Next Button")]
    public Button nextButton;

    [Header("File Settings")]
    public TextAsset questionFile;
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

    [Header("Answer Colors")]
    public Color correctColor = new Color(0.6f, 1f, 0.6f);
    public Color wrongColor = new Color(1f, 0.6f, 0.6f);
    public Color normalColor = Color.white;

    [Header("Audio / Slash SFX")]
    public bool useAnimationEventForSlash = false;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip slashClip;
    [Range(0f, 1f)] public float slashVolume = 1f;
    public Vector2 pitchJitter = new Vector2(0.98f, 1.02f);

    [Header("Target")]
    [SerializeField] private Scarecrow scarecrow;

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

    private void Awake()
    {
        if (!sfxSource)
        {
            if (knightAnimator && knightAnimator.TryGetComponent(out AudioSource knightSrc))
                sfxSource = knightSrc;
            else
                sfxSource = GetComponent<AudioSource>();
        }

        nextButton.gameObject.SetActive(false);
        nextButton.onClick.AddListener(GoToNextQuestion);
    }

    void Start()
    {
        resultPanel.SetActive(false);

        if (questionFile == null)
        {
            Debug.LogError("No question file assigned!");
            return;
        }

        allQuestions = LoadQuestionsFromFile();
        SelectBalancedQuestions();
        ShowQuestion();

        backToMenuButton.onClick.AddListener(() => SceneManager.LoadScene("MainMenu"));
    }

    //------------------------- File Loader -------------------------//
    private List<Question> LoadQuestionsFromFile()
    {
        List<Question> questions = new List<Question>();
        string[] lines = questionFile.text.Replace("\r", "").Split('\n');

        Question currentQuestion = null;
        List<string> questionTextBuffer = new List<string>();
        List<string> explanationBuffer = new List<string>();
        bool readingQuestion = false;
        bool readingExplanation = false;

        foreach (string raw in lines)
        {
            string line = raw.Trim();

            if (string.IsNullOrEmpty(line)) continue;

            if (line.StartsWith("Level:"))
            {
                if (currentQuestion != null)
                {
                    currentQuestion.questionText = string.Join("\n", questionTextBuffer);
                    currentQuestion.explanation = string.Join("\n", explanationBuffer);
                    questions.Add(currentQuestion);
                }

                currentQuestion = new Question();
                questionTextBuffer.Clear();
                explanationBuffer.Clear();
                readingQuestion = false;
                readingExplanation = false;
            }
            else if (line.StartsWith("Topic:"))
            {
                // optional: store topic if needed
            }
            else if (line.StartsWith("Q:"))
            {
                readingQuestion = true;
                readingExplanation = false;
                questionTextBuffer.Add(line.Substring(2).Trim());
            }
            else if (line.StartsWith("Explanation:"))
            {
                readingExplanation = true;
                readingQuestion = false;
                explanationBuffer.Add(line.Substring("Explanation:".Length).Trim());
            }
            else if (line.StartsWith("A)"))
            {
                currentQuestion.options = new string[4];
                currentQuestion.options[0] = line.Substring(2).Trim();
                readingQuestion = false;
                readingExplanation = false;
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
                currentQuestion.explanation = string.Join("\n", explanationBuffer);

                questions.Add(currentQuestion);

                currentQuestion = null;
                questionTextBuffer.Clear();
                explanationBuffer.Clear();
                readingQuestion = false;
                readingExplanation = false;
            }
            else
            {
                // Continuation lines:
                if (readingExplanation)
                {
                    explanationBuffer.Add(line);
                }
                else
                {
                    questionTextBuffer.Add(line);
                    readingQuestion = true;
                }
            }
        }

        // Save last question (if file doesn’t end with Answer:)
        if (currentQuestion != null)
        {
            currentQuestion.questionText = string.Join("\n", questionTextBuffer);
            currentQuestion.explanation = string.Join("\n", explanationBuffer);
            questions.Add(currentQuestion);
        }

        return questions;
    }

    //------------------------- Select Questions -------------------------//
    void SelectBalancedQuestions()
    {
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

    void Shuffle(List<Question> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (list[n], list[k]) = (list[k], list[n]);
        }
    }

    //------------------------- Show Question -------------------------//
    void ShowQuestion()
    {
        if (currentIndex >= quizQuestions.Count)
        {
            FinishQuiz();
            return;
        }

        Question q = quizQuestions[currentIndex];
        questionText.text = q.questionText;

        foreach (Button b in optionButtons)
        {
            b.gameObject.SetActive(true);
            b.interactable = true;
            b.image.color = normalColor;
        }

        feedbackText.text = "";
        explanationPanel.SetActive(false);
        nextButton.gameObject.SetActive(false);

        for (int i = 0; i < optionButtons.Length; i++)
        {
            int choiceIndex = i;
            optionButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = q.options[i];
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => OnOptionSelected(choiceIndex));
        }
            if (questionCounterText != null)
                questionCounterText.text = $"Question: {currentIndex + 1} / {totalQuestionsInTest}";
    }

    //------------------------- Handle Answer -------------------------//
    void OnOptionSelected(int choiceIndex)
    {
        Question q = quizQuestions[currentIndex];
        bool correct = choiceIndex == q.correctIndex;
        if (correct) score++;

        foreach (Button b in optionButtons)
            b.interactable = false;

        // Highlight answers
        optionButtons[q.correctIndex].image.color = correctColor;
        if (!correct) optionButtons[choiceIndex].image.color = wrongColor;
        for (int i = 0; i < optionButtons.Length; i++)
            if (i != q.correctIndex && i != choiceIndex) optionButtons[i].image.color = Color.gray;

        // Feedback & explanation
        feedbackText.text = correct ? "<color=green>Correct!</color>" : $"<color=red>Wrong.</color> Correct: {q.options[q.correctIndex]}";
        explanationPanel.SetActive(true);
        explanationText.text = string.IsNullOrEmpty(q.explanation) ?
            "Review the concept behind the correct answer." : q.explanation;

        if (correct)
        {
            if (knightAnimator) knightAnimator.SetTrigger("Attack");
            if (scarecrow) scarecrow.Wiggle();
            if (!useAnimationEventForSlash) PlaySlashSFX();
        }

        responses.Add(new QuestionResponse
        {
            questionText = q.questionText,
            selectedAnswer = q.options[choiceIndex],
            correctAnswer = q.options[q.correctIndex],
            isCorrect = correct
        });

        userAnswers.Add(choiceIndex);

        currentIndex++; // increment here, do NOT show next question yet
        nextButton.gameObject.SetActive(true); // Next button now closes explanation
    }

    //------------------------- Next Button -------------------------//
    void GoToNextQuestion()
    {
        if (currentIndex >= totalQuestionsInTest || currentIndex >= quizQuestions.Count)
        {
            FinishQuiz();
            return;
        }

        ShowQuestion();
    }

    //------------------------- Finish Quiz -------------------------//
    void FinishQuiz()
    {
        questionText.gameObject.SetActive(false);
        feedbackText.gameObject.SetActive(false);
        foreach (Button b in optionButtons) b.gameObject.SetActive(false);
        explanationPanel.SetActive(false);

        resultPanel.SetActive(true);
        resultText.text = $"Score: {score}/{quizQuestions.Count}";

        GenerateCSV();
    }

    //------------------------- CSV Generation -------------------------//
    void GenerateCSV()
    {
        string folderPath = Path.Combine(Application.dataPath, "QuizResults");
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        string filePath = Path.Combine(folderPath, $"Static_QuizResults_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv");

        string csvContent = "Question,SelectedAnswer,CorrectAnswer,Correct\n";
        for (int i = 0; i < quizQuestions.Count; i++)
        {
            Question q = quizQuestions[i];
            int userAnswer = i < userAnswers.Count ? userAnswers[i] : -1;
            bool correct = userAnswer == q.correctIndex;
            string qClean = q.questionText.Replace("\n", " ").Replace(",", " ");
            string sel = userAnswer >= 0 ? q.options[userAnswer].Replace(",", " ") : "";
            string corr = q.options[q.correctIndex].Replace(",", " ");
            csvContent += $"{qClean},{sel},{corr},{correct}\n";
        }
        csvContent += $"\nTotal Score,'{score}/{quizQuestions.Count}'";
        File.WriteAllText(filePath, csvContent);
        Debug.Log($"CSV saved at: {filePath}");
    }

    //------------------------- Audio -------------------------//
    public void PlaySlashSFX()
    {
        if (!slashClip) { Debug.LogWarning("Slash clip missing."); return; }
        if (!sfxSource) { Debug.LogWarning("AudioSource missing."); return; }

        sfxSource.pitch = Random.Range(pitchJitter.x, pitchJitter.y);
        sfxSource.PlayOneShot(slashClip, slashVolume);
    }

    //------------------------- Back To Menu -------------------------//
    public void BackToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
