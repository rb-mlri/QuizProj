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
    public Button[] optionButtons; // size = 4
    public TextMeshProUGUI feedbackText;
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;
    public Button backToMenuButton;

    [Header("File Settings")]
    public string baseFileName = "questions"; // file name without level number
    public int totalQuestionsInTest = 20;

    private List<Question> allQuestions = new List<Question>();
    private List<Question> quizQuestions = new List<Question>();
    private int currentIndex = 0;
    private int score = 0;

    void Start()
    {
        resultPanel.SetActive(false); // hide result panel at start

        int selectedLevel = PlayerPrefs.GetInt("SelectedLevel", 1);

        // Build the correct file name based on the selected level
        string fileName = $"{baseFileName}{selectedLevel}.txt";

        LoadQuestionsFromFile(fileName);
        SelectBalancedQuestions();
        ShowQuestion();

        backToMenuButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("MainMenu");
        });
    }

    void LoadQuestionsFromFile(string fileName)
    {
        string path = Path.Combine(Application.streamingAssetsPath, fileName);

        if (!File.Exists(path))
        {
            Debug.LogError("Questions file not found at: " + path);
            return;
        }

        string[] lines = File.ReadAllLines(path);
        Question q = null;
        List<string> opts = new List<string>();

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();
            if (string.IsNullOrEmpty(line))
            {
                if (q != null)
                {
                    q.options = opts.ToArray();
                    allQuestions.Add(q);
                }
                q = null;
                opts.Clear();
                continue;
            }

            if (line.StartsWith("Q:"))
            {
                q = new Question();
                q.questionText = line.Substring(2).Trim();
            }
            else if (line.StartsWith("A)") || line.StartsWith("B)") || line.StartsWith("C)") || line.StartsWith("D)"))
            {
                opts.Add(line.Substring(2).Trim());
            }
            else if (line.StartsWith("Answer:"))
            {
                if (q != null && int.TryParse(line.Substring(7).Trim(), out int idx))
                    q.correctIndex = idx;
            }
        }
    }

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
        quizQuestions.AddRange(easy.Take(7));
        quizQuestions.AddRange(medium.Take(7));
        quizQuestions.AddRange(hard.Take(6));
    }

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

    void OnOptionSelected(int choiceIndex)
    {
        Question q = quizQuestions[currentIndex];
        if (choiceIndex == q.correctIndex)
        {
            score++;
            if (feedbackText) feedbackText.text = "Correct!";
        }
        else
        {
            if (feedbackText) feedbackText.text = $"Wrong! Correct: {q.options[q.correctIndex]}";
        }

        currentIndex++;
        Invoke(nameof(ShowQuestion), 0.5f);
    }

    void FinishQuiz()
    {
        questionText.gameObject.SetActive(false);
        feedbackText.gameObject.SetActive(false);
        foreach (Button b in optionButtons) b.gameObject.SetActive(false);

        resultPanel.SetActive(true);
        resultText.text = $"Score: {score}/{quizQuestions.Count}";
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
