using UnityEngine;         // Provides access to Unity classes like MonoBehaviour, Vector3, Debug, etc.
using Commodore;           // Provides CommodoreBehavior for terminal input/output
using System.IO;           // Required for reading/writing files (save/load)
using System;              // Provides Exception handling classes

// ------------------- NumberGuessingGame -------------------
// Handles the text-based number guessing game and connects to Commodore terminal
public class NumberGuessingGame : CommodoreBehavior
{
    private GameManager gameManager;  // Holds the main game logic manager

    // Called once when the script is first run
    void Start()
    {
        gameManager = new GameManager(); // Create a new GameManager instance
        gameManager.StartNewGame();      // Start a new game immediately
    }

    // Called every frame
    void Update()
    {
        // Optional: detect if the player presses the Enter key
        if (Input.GetKeyDown(KeyCode.Return))
        {
            Debug.Log("Enter key pressed."); // Log Enter key for debugging
        }
    }

    // Called automatically by Commodore whenever the player types a command
    // Returns a string to display in the terminal
    protected override string ProcessCommand(string command)
    {
        try
        {
            return gameManager.ProcessCommand(command); // Forward input to GameManager
        }
        catch (Exception)
        {
            return "An unexpected error occurred. Try again."; // Catch any unexpected errors
        }
    }
}

// ------------------- GameManager -------------------
// Handles game flow, commands, and manages a NumberGuessingSession
public class GameManager
{
    // Enum to track the current state of the game
    public enum GameState { MainMenu, Playing, GameOver }

    private GameState currentState;            // Current state of the game
    private CommandProcessor commandProcessor; // Handles text commands
    private NumberGuessingSession session;     // Holds the current guessing session

    private int minNumber = 1;                 // Minimum number for guessing
    private int maxNumber = 10;                // Maximum number for guessing
    private string savePath;                    // File path for saving/loading

    // Constructor initializes the GameManager
    public GameManager()
    {
        currentState = GameState.MainMenu;              // Start at MainMenu
        commandProcessor = new CommandProcessor(this);  // Create a CommandProcessor
        savePath = Application.persistentDataPath + "/savegame.json"; // Save file path
    }

    // Starts a new game session
    public void StartNewGame()
    {
        session = new NumberGuessingSession(minNumber, maxNumber); // Create new session with min/max
        currentState = GameState.Playing;                           // Set state to Playing
    }

    // Ends the current game session
    public void EndGame()
    {
        currentState = GameState.GameOver; // Change state to GameOver
    }

    // Returns the current state
    public GameState GetCurrentState() => currentState;

    // Handles numeric guesses
    public string HandleGuess(int guess)
    {
        // Ensure the game is in Playing state
        if (currentState != GameState.Playing)
            return "You must start a game first. Type 'start'.";

        try
        {
            int result = session.CheckGuess(guess); // Check the guess against the session

            if (result == 0) // Correct guess
            {
                int target = session.GetTargetNumber(); // Get the correct number
                EndGame();                               // End the game
                return "Correct! The number was " + target + ". Game over.";
            }
            else if (result < 0) return "Too low! Try again.";  // Guess too low
            else return "Too high! Try again.";                 // Guess too high
        }
        catch (Exception)
        {
            return "An error occurred while processing your guess."; // Catch unexpected errors
        }
    }

    // ---------------- Save Game ----------------
    public string SaveGame()
    {
        // Check if a session exists
        if (session == null)
            return "No game to save. Start a game first.";

        try
        {
            string json = JsonUtility.ToJson(session); // Convert session to JSON string
            File.WriteAllText(savePath, json);         // Write JSON to file
            return "Game saved successfully.";
        }
        catch (Exception)
        {
            return "An error occurred while saving the game."; // Catch file errors
        }
    }

    // ---------------- Load Game ----------------
    public string LoadGame()
    {
        try
        {
            if (!File.Exists(savePath)) // Check if save file exists
                return "No saved game found.";

            string json = File.ReadAllText(savePath);                     // Read JSON from file
            var loadedSession = JsonUtility.FromJson<NumberGuessingSession>(json); // Deserialize JSON

            // Validate loaded data
            if (loadedSession.GetMin() <= 0 || loadedSession.GetMax() <= 0)
                return "Save file is corrupted or invalid.";

            session = loadedSession;                 // Set session
            currentState = GameState.Playing;        // Set state to Playing

            return $"Game loaded! Guess a number between {session.GetMin()} and {session.GetMax()}.";
        }
        catch (Exception)
        {
            return "An error occurred while loading the game."; // Catch file errors
        }
    }

    // Process player input through the CommandProcessor
    public string ProcessCommand(string input)
    {
        return commandProcessor.Process(input);
    }
}

// ------------------- CommandProcessor -------------------
// Interprets player commands and calls appropriate GameManager functions
public class CommandProcessor
{
    private GameManager gameManager; // Reference to GameManager

    // Constructor receives GameManager reference
    public CommandProcessor(GameManager manager)
    {
        gameManager = manager;
    }

    // Process text commands
    public string Process(string command)
    {
        string[] parts = command.ToLower().Trim().Split(' '); // Split command into words

        if (parts.Length == 0 || string.IsNullOrEmpty(parts[0]))
            return "Please type a command. Type 'help'."; // No command entered

        string mainCommand = parts[0]; // First word determines the command

        switch (mainCommand)
        {
            case "start": // Start a new game
                gameManager.StartNewGame();
                return $"Game started! Guess a number between 1 and 10.";

            case "guess": // Make a guess
                if (gameManager.GetCurrentState() != GameManager.GameState.Playing)
                    return "You must start a game first. Type 'start'.";

                if (parts.Length < 2) return "Usage: guess <number>";

                try
                {
                    if (int.TryParse(parts[1], out int guess)) // Try parse number
                        return gameManager.HandleGuess(guess); // Forward to GameManager
                    else
                        return "Please enter a valid number."; // Invalid input
                }
                catch (Exception)
                {
                    return "An error occurred while processing your guess."; // Catch unexpected errors
                }

            case "restart": // Restart the game
                gameManager.StartNewGame();
                return "Game restarted! Guess a number between 1 and 10.";

            case "quit": // End the game
                gameManager.EndGame();
                return "Game ended. Type 'start' to play again.";

            case "help": // Show command list
                return "Commands:\nstart\nguess <number>\nrestart\nquit\nsave\nload\nhelp";

            case "save": // Save game
                return gameManager.SaveGame();

            case "load": // Load game
                return gameManager.LoadGame();

            default: // Unknown command
                return "Unknown command. Type 'help'.";
        }
    }
}

// ------------------- NumberGuessingSession -------------------
// Stores a single game session and target number
[System.Serializable] // Allows JSON serialization
public class NumberGuessingSession
{
    private int targetNumber; // Secret number to guess
    private int min;          // Minimum number
    private int max;          // Maximum number

    // Constructor sets target number randomly between min and max
    public NumberGuessingSession(int min, int max)
    {
        this.min = min;
        this.max = max;
        targetNumber = UnityEngine.Random.Range(min, max + 1); // Random.Range is inclusive min, exclusive max+1
    }

    // Check a guess: 0 = correct, -1 = too low, 1 = too high
    public int CheckGuess(int guess)
    {
        if (guess == targetNumber) return 0;
        else if (guess < targetNumber) return -1;
        else return 1;
    }

    // Getters to expose private variables safely
    public int GetTargetNumber() => targetNumber;
    public int GetMin() => min;
    public int GetMax() => max;
}
