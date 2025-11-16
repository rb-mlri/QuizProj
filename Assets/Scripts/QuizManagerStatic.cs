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
    [Tooltip("Assign the question text file manually in the Inspector")]
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
    [SerializeField] private Animator knightAnimator;   // assign your Knight Animator

    [Header("Audio / Slash SFX")]
    [Tooltip("If true, sound will be triggered by an Animation Event calling PlaySlashSFX(). If false, it will play immediately when the answer is correct.")]
    public bool useAnimationEventForSlash = false;
    [SerializeField] private AudioSource sfxSource;     // ideally on the Knight
    [SerializeField] private AudioClip slashClip;       // your slash.wav / mp3
    [Range(0f, 1f)] public float slashVolume = 1f;
    public Vector2 pitchJitter = new Vector2(0.98f, 1.02f);

    [Header("Target")]
    [SerializeField] private Scarecrow scarecrow;       // optional wobble

    [Header("Timing Settings")]
    public float nextQuestionDelay = 2f;

    // CSV Generator
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
            // Prefer the knight object if available
            if (knightAnimator && knightAnimator.TryGetComponent(out AudioSource knightSrc))
                sfxSource = knightSrc;
            else
                sfxSource = GetComponent<AudioSource>(); // fallback to this GameObject
        }
    }

    void Start()
    {
        resultPanel.SetActive(false);

        if (questionFile == null)
        {
            Debug.LogError("[StaticQuiz] ❌ No question file assigned in Inspector!");
            return;
        }

        allQuestions = LoadQuestionsFromFile();

        if (allQuestions == null || allQuestions.Count == 0)
        {
            Debug.LogError("[StaticQuiz] ❌ No valid questions found in the file!");
            return;
        }

        SelectBalancedQuestions();
        ShowQuestion();

        backToMenuButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("MainMenu");
        });
    }

    //------------------------- File Loader -------------------------//
    private List<Question> LoadQuestionsFromFile()
    {
        List<Question> questions = new List<Question>();
        string[] lines = questionFile.text.Replace("\r", "").Split('\n');

        Question currentQuestion = null;
        List<string> questionTextBuffer = new List<string>();

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();
            if (string.IsNullOrEmpty(line)) continue;

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

        Debug.Log($"[StaticQuiz] ✅ Loaded {questions.Count} questions from {questionFile.name}");
        return questions;
    }

    //------------------------- Select Balanced Questions -------------------------//
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

        Debug.Log($"[StaticQuiz] 🧩 Selected {quizQuestions.Count} total questions");
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
            (list[n], list[k]) = (list[k], list[n]);
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
        if (q == null)
        {
            Debug.LogError($"[StaticQuiz] ⚠️ Null question at index {currentIndex}");
            FinishQuiz();
            return;
        }

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

    //------------------------- Answer Checker -------------------------//
    void OnOptionSelected(int choiceIndex)
    {
        Question q = quizQuestions[currentIndex];
        bool correct = choiceIndex == q.correctIndex;

        foreach (Button b in optionButtons)
            b.interactable = false;

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

            if (knightAnimator) knightAnimator.SetTrigger("Attack");


            // Play slash either now (immediate) or let the Animation Event call PlaySlashSFX()
            if (!useAnimationEventForSlash)
                PlaySlashSFX();


            if (sfxSource && slashClip) sfxSource.PlayOneShot(slashClip);

            if (scarecrow) scarecrow.Wiggle();
        }
        else if (feedbackText)
        {
            feedbackText.text = $"Wrong! Correct: {q.options[q.correctIndex]}";
        }

        currentIndex++;
        userAnswers.Add(choiceIndex);

        StartCoroutine(NextQuestionAfterDelay());
    }

    IEnumerator NextQuestionAfterDelay()
    {
        yield return new WaitForSeconds(nextQuestionDelay);

        foreach (Button b in optionButtons)
            b.interactable = true;

        if (currentIndex < quizQuestions.Count)
            ShowQuestion();
        else
            FinishQuiz();
    }

    //------------------------- Finish Quiz -------------------------//
    void FinishQuiz()
    {
        questionText.gameObject.SetActive(false);
        if (feedbackText) feedbackText.gameObject.SetActive(false);
        foreach (Button b in optionButtons) b.gameObject.SetActive(false);

        resultPanel.SetActive(true);
        resultText.text = $"Score: {score}/{quizQuestions.Count}";
        GenerateCSV();
    }

    //------------------------- CSV Generator -------------------------//
    void GenerateCSV()
    {
        string mode = "Static";
        int level = PlayerPrefs.GetInt("SelectedLevel", 1);

        string folderPath = Path.Combine(Application.dataPath, "QuizResults");
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        string filePath = Path.Combine(folderPath, $"{mode}_QuizResults_Level{level}_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv");

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

        csvContent += $"\nTotal Score,'{score}/{quizQuestions.Count}'";

        File.WriteAllText(filePath, csvContent);
        Debug.Log($"[StaticQuiz] 📁 CSV saved at: {filePath}");
    }

    //------------------------- Back To Menu -------------------------//
    public void BackToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    // ------------------------ AUDIO: called from Animation Event or directly ------------------------ //
    /// <summary>
    /// Plays the sword slash SFX. Hook this from the Knight Attack animation via an Animation Event.
    /// </summary>
    public void PlaySlashSFX()
    {
        if (!slashClip)
        {
            Debug.LogWarning("[StaticQuiz] Slash clip not assigned.");
            return;
        }

        if (!sfxSource)
        {
            Debug.LogWarning("[StaticQuiz] AudioSource not found. Add one to the Knight or assign it here.");
            return;
        }

        sfxSource.pitch = Random.Range(pitchJitter.x, pitchJitter.y);
        sfxSource.PlayOneShot(slashClip, slashVolume);
    }
}