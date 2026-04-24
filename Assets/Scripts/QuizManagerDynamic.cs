using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;

public class QuizManagerDynamic : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI questionText;
    public Button[] optionButtons;
    public TextMeshProUGUI feedbackText;
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI knowledgeText;
    public Button backToMenuButton;
    public TextMeshProUGUI questionCounterText;
    public TextMeshProUGUI statusText;

    [Header("Next Button & Explanation")]
    public Button nextButton;
    public GameObject explanationPanel;
    public TextMeshProUGUI explanationText;

    [Header("Answer Colors")]
    public Color correctColor = new Color(0.6f, 1f, 0.6f);
    public Color wrongColor = new Color(1f, 0.6f, 0.6f);
    public Color normalColor = Color.white;

    [Header("File Settings")]
    public TextAsset questionFile;

    [Header("Game Settings")]
    public int totalQuestions = 20;
    public Animator knightAnimator;
    public Scarecrow scarecrow;
    public AudioSource audioSource;
    public AudioClip slashSound;

    private List<Question> allQuestions = new List<Question>();
    private HashSet<string> askedTopics = new HashSet<string>();
    private HashSet<string> usedQuestions = new HashSet<string>();
    private List<string> allTopics = new List<string>();

    private Difficulty currentDifficulty = Difficulty.Easy;
    private int currentIndex = 0;
    private int score = 0;

    private Dictionary<string, float> knowledgeStates = new Dictionary<string, float>();

    [System.Serializable]
    public class QuestionResponse
    {
        public string questionText;
        public string selectedAnswer;
        public string correctAnswer;
        public bool isCorrect;
        public Difficulty difficulty;
        public string topic;
        public float weight;
    }

    private List<QuestionResponse> responses = new List<QuestionResponse>();

    void Awake()
    {
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(false);
            nextButton.onClick.AddListener(GoToNextQuestion);
        }
    }

    void Start()
    {
        resultPanel.SetActive(false);
        explanationPanel.SetActive(false);

        if (questionFile == null)
        {
            Debug.LogError("No question file assigned in Inspector!");
            return;
        }

        LoadQuestionsFromFile();

        foreach (var q in allQuestions)
        {
            if (!string.IsNullOrEmpty(q.topic) && !knowledgeStates.ContainsKey(q.topic))
            {
                knowledgeStates[q.topic] = 0.3f;
                allTopics.Add(q.topic);
            }
        }

        ShowNextQuestion();
        backToMenuButton.onClick.AddListener(() => SceneManager.LoadScene("MainMenu"));
    }

    //------------------------- File Loader -------------------------//
    void LoadQuestionsFromFile()
    {
        string[] lines = questionFile.text.Replace("\r", "").Split('\n');

        Question currentQuestion = null;
        List<string> questionTextBuffer = new List<string>();
        List<string> explanationBuffer = new List<string>();
        bool readingExplanation = false;

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();
            if (string.IsNullOrEmpty(line)) continue;

            if (line.StartsWith("Level:"))
            {
                if (currentQuestion != null)
                {
                    currentQuestion.questionText = string.Join("\n", questionTextBuffer);
                    currentQuestion.explanation = string.Join("\n", explanationBuffer);
                    allQuestions.Add(currentQuestion);
                }

                currentQuestion = new Question();
                questionTextBuffer.Clear();
                explanationBuffer.Clear();
                readingExplanation = false;
            }
            else if (line.StartsWith("Topic:"))
            {
                currentQuestion.topic = line.Split(':')[1].Trim();
            }
            else if (line.StartsWith("Q:"))
            {
                string questionLine = line.Substring(2).Trim();
                questionTextBuffer.Add(questionLine);

                if (questionLine.Contains("(Easy)"))
                    currentQuestion.difficulty = Difficulty.Easy;
                else if (questionLine.Contains("(Medium)"))
                    currentQuestion.difficulty = Difficulty.Medium;
                else if (questionLine.Contains("(Hard)"))
                    currentQuestion.difficulty = Difficulty.Hard;
                else
                    currentQuestion.difficulty = Difficulty.Easy;

                readingExplanation = false;
            }
            else if (line.StartsWith("Explanation:"))
            {
                explanationBuffer.Add(line.Substring("Explanation:".Length).Trim());
                readingExplanation = true;
            }
            else if (line.StartsWith("A)"))
            {
                currentQuestion.options = new string[4];
                currentQuestion.options[0] = line.Substring(2).Trim();
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

                allQuestions.Add(currentQuestion);

                currentQuestion = null;
                questionTextBuffer.Clear();
                explanationBuffer.Clear();
                readingExplanation = false;
            }
            else
            {
                if (currentQuestion != null)
                {
                    if (readingExplanation)
                        explanationBuffer.Add(line);
                    else
                        questionTextBuffer.Add(line);
                }
            }
        }

        if (currentQuestion != null)
        {
            currentQuestion.questionText = string.Join("\n", questionTextBuffer);
            currentQuestion.explanation = string.Join("\n", explanationBuffer);
            allQuestions.Add(currentQuestion);
        }
    }

    //------------------------- Educational Topic Priority -------------------------//
    string SelectTopicByEducationalPriority()
    {
        if (knowledgeStates.Count == 0) return null;

        var weights = new Dictionary<string, float>();
        float totalWeight = 0f;

        foreach (var kvp in knowledgeStates)
        {
            // Higher priority if mastery is low
            float priority = Mathf.Clamp01(1f - kvp.Value);

            // High mastery topics still appear (but less frequently)
            priority = Mathf.Max(priority, 0.2f);

            weights[kvp.Key] = priority;
            totalWeight += priority;
        }

        float r = Random.value * totalWeight;
        float cumulative = 0f;

        foreach (var kvp in weights)
        {
            cumulative += kvp.Value;
            if (r <= cumulative)
                return kvp.Key;
        }

        return knowledgeStates.Keys.FirstOrDefault();
    }

    Difficulty GetDifficultyForTopic(string topic)
    {
        float mastery = knowledgeStates.ContainsKey(topic) ? knowledgeStates[topic] : 0.3f;

        if (mastery < 0.40f)
            return Difficulty.Easy;
        if (mastery < 0.70f)
            return Difficulty.Medium;

        return Difficulty.Hard;
    }

    //------------------------- Display Questions -------------------------//
    void ShowNextQuestion()
    {
        if (currentIndex >= totalQuestions)
        {
            FinishQuiz();
            return;
        }

        string topic = SelectTopicByEducationalPriority();
        if (string.IsNullOrEmpty(topic))
            topic = allTopics.Count > 0 ? allTopics[Random.Range(0, allTopics.Count)] : null;

        Difficulty difficulty = GetDifficultyForTopic(topic);

        List<Question> pool = allQuestions
            .Where(q => q.topic == topic && q.difficulty == difficulty)
            .ToList();

        if (pool.Count == 0)
            pool = allQuestions.Where(q => q.topic == topic).ToList();

        if (pool.Count == 0)
            pool = allQuestions;

        if (pool.Count == 0)
        {
            Debug.LogError("No questions available.");
            FinishQuiz();
            return;
        }

        Question q = pool[Random.Range(0, pool.Count)];

        // weight assignment for CSV (and educational tracking)
        switch (q.difficulty)
        {
            case Difficulty.Easy: q.weight = 0.25f; break;
            case Difficulty.Medium: q.weight = 0.60f; break;
            case Difficulty.Hard: q.weight = 1.00f; break;
            default: q.weight = 0.25f; break;
        }

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
            optionButtons[i].onClick.AddListener(() => OnOptionSelected(q, choiceIndex));
        }

        if (questionCounterText != null)
            questionCounterText.text = $"Question: #{currentIndex + 1}";

        usedQuestions.Add(q.questionText);
        currentDifficulty = q.difficulty;
        askedTopics.Add(topic);

        if (statusText != null)
            statusText.text = $"Level: {currentDifficulty} | Question: #{currentIndex + 1}";
    }

    //------------------------- Answer Handling -------------------------//
    void OnOptionSelected(Question q, int choiceIndex)
    {
        bool correct = choiceIndex == q.correctIndex;
        if (correct) score++;

        foreach (Button b in optionButtons)
            b.interactable = false;

        optionButtons[q.correctIndex].image.color = correctColor;
        if (!correct)
            optionButtons[choiceIndex].image.color = wrongColor;

        for (int i = 0; i < optionButtons.Length; i++)
            if (i != q.correctIndex && i != choiceIndex)
                optionButtons[i].image.color = Color.gray;

        feedbackText.text = correct ? "<color=green>Correct!</color>" :
            $"<color=red>Wrong.</color> Correct: {q.options[q.correctIndex]}";

        explanationPanel.SetActive(true);
        explanationText.text = string.IsNullOrEmpty(q.explanation)
            ? "Review the concept behind the correct answer."
            : q.explanation;

        if (correct)
        {
            if (knightAnimator) knightAnimator.SetTrigger("Attack");
            if (scarecrow) scarecrow.Wiggle();
            if (audioSource && slashSound) audioSource.PlayOneShot(slashSound);
        }

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

        UpdateBayesianKnowledge(q.topic, correct, q.weight);

        currentIndex++;
        nextButton.gameObject.SetActive(true);
    }

    void GoToNextQuestion()
    {
        if (currentIndex >= totalQuestions)
        {
            FinishQuiz();
            return;
        }
        ShowNextQuestion();
    }

    //------------------------- Knowledge Update (Educational) -------------------------//
    void UpdateBayesianKnowledge(string topic, bool wasCorrect, float weight)
    {
        if (string.IsNullOrEmpty(topic)) return;
        if (!knowledgeStates.ContainsKey(topic))
            knowledgeStates[topic] = 0.3f;

        float prior = knowledgeStates[topic];

        float Lc = 0.4f + 0.2f * weight;
        float Ln = 0.05f + 0.05f * (1f - weight);

        float posterior = wasCorrect
            ? prior + (1f - prior) * Lc
            : prior - prior * Ln;

        knowledgeStates[topic] = Mathf.Clamp(posterior, 0.05f, 1f);

        Debug.Log($"[Topic Mastery] {topic}: {prior:P0} → {knowledgeStates[topic]:P0}");
    }

    //------------------------- Finish Quiz -------------------------//
    void FinishQuiz()
    {
        questionText.gameObject.SetActive(false);
        feedbackText.gameObject.SetActive(false);
        foreach (Button b in optionButtons) b.gameObject.SetActive(false);
        explanationPanel.SetActive(false);
        nextButton.gameObject.SetActive(false);

        resultPanel.SetActive(true);
        resultText.text = $"Score: {score}/{totalQuestions}";
        ShowKnowledgeStatesOnResultPanel();
        GenerateCSV();
    }

    void ShowKnowledgeStatesOnResultPanel()
    {
        if (knowledgeText == null) return;

        string display = "Topic Mastery (Estimates):\n";
        foreach (var kvp in knowledgeStates)
            display += $"{kvp.Key}: {(kvp.Value * 100f):F1}%\n";

        knowledgeText.text = display;
    }

    //------------------------- CSV (Correct Difficulty & Weight) -------------------------//
    void GenerateCSV()
    {
        string mode = "Dynamic";
        int level = PlayerPrefs.GetInt("SelectedLevel", 1);

        string folderPath = Path.Combine(Application.dataPath, "QuizResults");
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        string filePath = Path.Combine(folderPath,
            $"{mode}_QuizResults_Level{level}_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv");

        string csvContent = "Question,Topic,Difficulty,Weight,SelectedAnswer,CorrectAnswer,Correct\n";

        foreach (var r in responses)
        {
            string questionTextClean = r.questionText.Replace("\n", " ").Replace(",", " ");
            string topicClean = r.topic?.Replace(",", " ") ?? "";
            string selectedAnswerText = r.selectedAnswer.Replace(",", " ");
            string correctAnswerText = r.correctAnswer.Replace(",", " ");

            csvContent += $"{questionTextClean},{topicClean},{r.difficulty},{r.weight:F2},{selectedAnswerText},{correctAnswerText},{r.isCorrect}\n";
        }

        csvContent += $"\nTotal Score,'{score}/{totalQuestions}'\n";

        foreach (var kvp in knowledgeStates)
            csvContent += $"{kvp.Key},{kvp.Value:F2}\n";

        File.WriteAllText(filePath, csvContent);
        Debug.Log("CSV saved at: " + filePath);
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}