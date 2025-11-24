using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Collections;

public class QuizManagerDynamic : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI questionText;
    public Button[] optionButtons; // size = 4
    public TextMeshProUGUI feedbackText;
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI knowledgeText;
    public Button backToMenuButton;

    [Header("File Settings")]
    public TextAsset questionFile;

    [Header("Adaptive Difficulty Settings")]
    public int correctToLevelUp = 3;
    public int wrongToLevelDown = 2;
    public int totalQuestions = 20;

    private List<Question> allQuestions = new List<Question>();
    private List<Question> easyPool = new List<Question>();
    private List<Question> mediumPool = new List<Question>();
    private List<Question> hardPool = new List<Question>();

    private Difficulty currentDifficulty = Difficulty.Easy;
    private int currentIndex = 0;
    private int score = 0;
    private int easyCorrect = 0, mediumCorrect = 0, hardCorrect = 0;
    private int wrongInCurrentDifficulty = 0;

    [Header("Game")]
    public Animator knightAnimator;
    public Scarecrow scarecrow;
    public AudioSource audioSource;
    public AudioClip slashSound;

    [Header("Timing Settings")]
    public float nextQuestionDelay = 2f;


    // Bayesian knowledge states
    private Dictionary<string, float> knowledgeStates = new Dictionary<string, float>();

    // CSV Generator
    [System.Serializable]
    public class QuestionResponse
    {
        public string questionText;
        public string selectedAnswer;
        public string correctAnswer;
        public bool isCorrect;
        public Difficulty difficulty;
        public string topic;
        public int weight;
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

        LoadQuestionsFromFile();
        SplitQuestionsByDifficulty();

        // Initialize all topics at 50% baseline
        foreach (var q in allQuestions)
        {
            if (!string.IsNullOrEmpty(q.topic) && !knowledgeStates.ContainsKey(q.topic))
                knowledgeStates[q.topic] = 0.5f;
        }


        ShowNextQuestion();

        backToMenuButton.onClick.AddListener(() => SceneManager.LoadScene("MainMenu"));
    }

    //------------------------- File Loader (Ex. questions'#'.txt) -------------------------//
    void LoadQuestionsFromFile()
    {
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
                    allQuestions.Add(currentQuestion);
                }

                currentQuestion = new Question();
                questionTextBuffer.Clear();
            }
            else if (line.StartsWith("Q:"))
            {
                string questionLine = line.Substring(2).Trim();
                questionTextBuffer.Add(questionLine);

                if (questionLine.Contains("(Easy)")) currentQuestion.difficulty = Difficulty.Easy;
                else if (questionLine.Contains("(Medium)")) currentQuestion.difficulty = Difficulty.Medium;
                else if (questionLine.Contains("(Hard)")) currentQuestion.difficulty = Difficulty.Hard;
            }
            else if (line.StartsWith("Topic:"))
            {
                if (currentQuestion != null)
                    currentQuestion.topic = line.Split(':')[1].Trim();
            }
            else if (line.StartsWith("Weight:"))
            {
                if (currentQuestion != null)
                {
                    int parsedWeight;
                    if (int.TryParse(line.Split(':')[1].Trim(), out parsedWeight))
                        currentQuestion.weight = Mathf.Clamp(parsedWeight, 1, 3);
                }
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
                allQuestions.Add(currentQuestion);
                currentQuestion = null;
                questionTextBuffer.Clear();
            }
            else
            {
                if (currentQuestion != null)
                    questionTextBuffer.Add(line);
            }
        }
    }

    //------------------------- Split Into Categories By Difficulty -------------------------//
    void SplitQuestionsByDifficulty()
    {
        easyPool = allQuestions.Where(q => q.difficulty == Difficulty.Easy).ToList();
        mediumPool = allQuestions.Where(q => q.difficulty == Difficulty.Medium).ToList();
        hardPool = allQuestions.Where(q => q.difficulty == Difficulty.Hard).ToList();

        Shuffle(easyPool);
        Shuffle(mediumPool);
        Shuffle(hardPool);
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
            var temp = list[k];
            list[k] = list[n];
            list[n] = temp;
        }
    }

    //------------------------- Display Questions -------------------------//
    void ShowNextQuestion()
    {
        if (currentIndex >= totalQuestions)
        {
            FinishQuiz();
            return;
        }

        Question q = GetQuestionFromDifficulty(currentDifficulty);
        if (q == null) q = allQuestions[Random.Range(0, allQuestions.Count)];

        questionText.text = q.questionText;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            int choiceIndex = i;
            optionButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = q.options[i];
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => OnOptionSelected(q, choiceIndex));
        }

        if (feedbackText) feedbackText.text = "";
    }

    //------------------------- Pick From Pool Of Questions -------------------------//
    Question GetQuestionFromDifficulty(Difficulty diff)
    {
        List<Question> pool = diff switch
        {
            Difficulty.Easy => easyPool,
            Difficulty.Medium => mediumPool,
            Difficulty.Hard => hardPool,
            _ => easyPool
        };

        if (pool.Count == 0)
            pool = allQuestions;

        // 1️⃣ Choose a topic based on Bayesian confidence
        string selectedTopic = SelectTopicByConfidence();

        // 2️⃣ Prefer questions from that topic
        List<Question> topicQuestions = pool
            .Where(q => q.topic == selectedTopic)
            .ToList();

        // 3️⃣ Fallback if that topic pool is empty
        if (topicQuestions.Count == 0)
            topicQuestions = pool;

        // 4️⃣ Choose a random question
        Question q = topicQuestions[Random.Range(0, topicQuestions.Count)];
        pool.Remove(q);

        // 5️⃣ Dynamically adjust difficulty based on mastery
        float mastery = knowledgeStates.ContainsKey(q.topic) ? knowledgeStates[q.topic] : 0.5f;

        if (mastery >= 0.8f && currentDifficulty != Difficulty.Hard)
            currentDifficulty = Difficulty.Hard;
        else if (mastery >= 0.6f && currentDifficulty == Difficulty.Easy)
            currentDifficulty = Difficulty.Medium;
        else if (mastery <= 0.4f && currentDifficulty != Difficulty.Easy)
            currentDifficulty = Difficulty.Easy;

        return q;
    }

    //------------------------- Select Topic w/ Confidence -------------------------//

    string SelectTopicByConfidence()
    {
        if (knowledgeStates.Count == 0)
            return null;

        var weights = new Dictionary<string, float>();
        float totalWeight = 0f;

        foreach (var kvp in knowledgeStates)
        {
            // Topics with low confidence (low knowledge) get higher weight
            float w = Mathf.Max(0.01f, 1f - kvp.Value);
            weights[kvp.Key] = w;
            totalWeight += w;
        }

        float r = Random.value * totalWeight;
        float cum = 0f;

        foreach (var kvp in weights)
        {
            cum += kvp.Value;
            if (r <= cum)
                return kvp.Key;
        }

        return knowledgeStates.Keys.FirstOrDefault();
    }



    //------------------------- Chosen Answer Checker (Ex. If Right/Wrong) -------------------------//
    void OnOptionSelected(Question q, int choiceIndex)
    {
        bool correct = choiceIndex == q.correctIndex;
        if (correct) score++;

        // Disable buttons immediately to prevent rapid clicking
        foreach (Button b in optionButtons)
            b.interactable = false;

        responses.Add(new QuestionResponse
        {
            questionText = q.questionText,
            selectedAnswer = q.options[choiceIndex],
            correctAnswer = q.options[q.correctIndex],
            isCorrect = correct,
            difficulty = q.difficulty,
            topic = q.topic,
            weight = q.weight
        });

        userAnswers.Add(choiceIndex);

        if (feedbackText)
            feedbackText.text = correct ? "Correct!" : $"Wrong! Correct: {q.options[q.correctIndex]}";

        if (correct)
        {
            if (knightAnimator) knightAnimator.SetTrigger("Attack");
            if (scarecrow) scarecrow.Wiggle();
            if (audioSource && slashSound) audioSource.PlayOneShot(slashSound);
        }

        UpdateBayesianKnowledge(q.topic, correct, q.weight);
        UpdateDifficultyAfterAnswer(correct);

        currentIndex++;

        // Delay showing next question
        StartCoroutine(NextQuestionAfterDelay());
    }

    // Delays Next Question 
    IEnumerator NextQuestionAfterDelay()
    {
        yield return new WaitForSeconds(nextQuestionDelay);

        // Re-enable buttons before next question
        foreach (Button b in optionButtons)
            b.interactable = true;

        ShowNextQuestion();
    }

    //------------------------- Bayesian Network | Question Weighting -------------------------//
    void UpdateBayesianKnowledge(string topic, bool wasCorrect, int weight)
    {
        if (string.IsNullOrEmpty(topic)) return;

        if (!knowledgeStates.ContainsKey(topic))
            knowledgeStates[topic] = 0.5f; // start neutral

        float prior = knowledgeStates[topic];

        // Base likelihoods
        float baseLc = 0.85f;  // chance of correct if topic is known
        float baseLn = 0.20f;  // chance of correct if topic is NOT known

        // Adjust confidence by weight — harder = more informative
        float weightFactor = (weight - 1) * 0.2f; // 0 (easy), 0.2 (med), 0.4 (hard)
        float Lc = Mathf.Clamp01(baseLc + weightFactor * 0.10f);
        float Ln = Mathf.Clamp01(baseLn - weightFactor * 0.10f);

        float posterior = prior;

        if (wasCorrect)
        {
            float numerator = prior * Lc;
            float denominator = numerator + (1f - prior) * Ln;
            if (denominator > 0f) posterior = numerator / denominator;
        }
        else
        {
            float numerator = prior * (1f - Lc);
            float denominator = numerator + (1f - prior) * (1f - Ln);
            if (denominator > 0f) posterior = numerator / denominator;
        }

        posterior = Mathf.Clamp01(posterior);
        knowledgeStates[topic] = posterior;

        Debug.Log($"[Topic Mastery] {topic}: {prior:P0} → {posterior:P0} (Correct: {wasCorrect})");

    }


    //------------------------- Decision Tree | Difficulty Adjustment -------------------------//
    void UpdateDifficultyAfterAnswer(bool wasCorrect)
    {
        if (wasCorrect)
        {
            switch (currentDifficulty)
            {
                case Difficulty.Easy: easyCorrect++; break;
                case Difficulty.Medium: mediumCorrect++; break;
                case Difficulty.Hard: hardCorrect++; break;
            }
        }
        else
        {
            wrongInCurrentDifficulty++;
        }

        if (currentDifficulty == Difficulty.Easy && easyCorrect >= correctToLevelUp)
        {
            currentDifficulty = Difficulty.Medium;
            wrongInCurrentDifficulty = 0;
        }
        else if (currentDifficulty == Difficulty.Medium && mediumCorrect >= correctToLevelUp)
        {
            currentDifficulty = Difficulty.Hard;
            wrongInCurrentDifficulty = 0;
        }

        if (currentDifficulty == Difficulty.Medium && wrongInCurrentDifficulty >= wrongToLevelDown)
        {
            currentDifficulty = Difficulty.Easy;
            wrongInCurrentDifficulty = 0;
        }
        else if (currentDifficulty == Difficulty.Hard && wrongInCurrentDifficulty >= wrongToLevelDown)
        {
            currentDifficulty = Difficulty.Medium;
            wrongInCurrentDifficulty = 0;
        }
    }

    //------------------------- Quiz End -------------------------//
    void FinishQuiz()
    {
        questionText.gameObject.SetActive(false);
        feedbackText.gameObject.SetActive(false);
        foreach (Button b in optionButtons) b.gameObject.SetActive(false);

        resultPanel.SetActive(true);
        resultText.text = $"Score: {score}/{totalQuestions}";

        ShowKnowledgeStatesOnResultPanel();
        GenerateCSV();
    }

    //------------------------- Show Knowledge States -------------------------//
    void ShowKnowledgeStatesOnResultPanel()
    {
        if (knowledgeText == null) return;

        string display = "Topic Mastery (Estimates):\n";
        foreach (var kvp in knowledgeStates)
        {
            display += $"{kvp.Key}: {(kvp.Value * 100f):F1}%\n";
        }

        knowledgeText.text = display;
    }

    //------------------------- CSV Generation -------------------------//
    void GenerateCSV()
    {
        string mode = "Dynamic";
        int level = PlayerPrefs.GetInt("SelectedLevel", 1);

        string folderPath = Path.Combine(Application.dataPath, "QuizResults");
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        string filePath = Path.Combine(folderPath, $"{mode}_QuizResults_Level{level}_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv");

        // Header without Difficulty
        string csvContent = "Question,Topic,Weight,SelectedAnswer,CorrectAnswer,Correct\n";

        for (int i = 0; i < responses.Count; i++)
        {
            var r = responses[i];
            string questionTextClean = r.questionText.Replace("\n", " ").Replace(",", " ");
            string topicClean = r.topic.Replace(",", " ");
            string selectedAnswerText = r.selectedAnswer.Replace(",", " ");
            string correctAnswerText = r.correctAnswer.Replace(",", " ");

            csvContent += $"{questionTextClean},{topicClean},{r.weight},{selectedAnswerText},{correctAnswerText},{r.isCorrect}\n";
        }

        // Prefix score with a single quote to prevent Excel from interpreting as a date
        csvContent += $"\nTotal Score,'{score}/{totalQuestions}'\n";

        csvContent += "Knowledge States:\n";
        foreach (var kvp in knowledgeStates)
        {
            csvContent += $"{kvp.Key},{kvp.Value:F2}\n";
        }

        File.WriteAllText(filePath, csvContent);
        Debug.Log("CSV saved at: " + filePath);
    }


    //------------------------- Main Menu -------------------------//
    public void BackToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}