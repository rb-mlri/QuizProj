using UnityEngine;

[System.Serializable]

public enum Difficulty
{
    Easy,
    Medium,
    Hard
}
public class Question
{
    public string questionText;
    public string[] options;
    public int correctIndex;
    public Difficulty difficulty;
    public string topic;
    public int weight = 1;
}


