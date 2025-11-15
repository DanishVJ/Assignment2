using UnityEngine;         // Provides access to Unity’s core classes (like MonoBehaviour, Vector3, Debug, etc.)
using Commodore;           // Provides access to CommodoreBehavior and terminal functionality
using System.IO;           // Required for file operations (save/load JSON)

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
            Debug.Log("Enter key pressed! Could trigger additional actions here.");
        }
    }

    // Called automatically by Commodore whenever the player types a command
    // Returns a string to display in the terminal
    protected override string ProcessCommand(string command)
    {
        return gameManager.ProcessCommand(command); // Forward input to GameManager
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
        commandProcessor = new CommandProcessor(this);  // Create a CommandProcessor and pass reference to this GameManager
        savePath = Application.persistentDataPath + "/savegame.json"; // Path for save/load file
    }

    // Starts a new game session
    public void StartNewGame()
    {
        session = new NumberGuessingSession(minNumber, maxNumber); // Create a new session
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
            return "You must start a game first! Type 'start' to begin.";

        int result = session.CheckGuess(guess); // Check the guess

        if (result == 0) // Correct guess
        {
            int target = session.GetTargetNumber(); // Get the correct number
            EndGame();                               // End the game
            return $"🎉 Correct! The number was {target}. Game over!";
        }
        else if (result < 0) // Guess too low
        {
            return "Too low! Try again.";
        }
        else // Guess too high
        {
            return "Too high! Try again.";
        }
    }

    // -------------------- Save Game --------------------
    public string SaveGame()
    {
        if (session == null) // No session to save
            return "No game to save.";

        string json = JsonUtility.ToJson(session); // Convert session to JSON string
        File.WriteAllText(savePath, json);         // Write JSON to file
        return $"Game saved to {savePath}";       // Confirm save
    }

    // -------------------- Load Game --------------------
    public string LoadGame()
    {
        if (!File.Exists(savePath)) // Check if save file exists
            return "No saved game found.";

        string json = File.ReadAllText(savePath);                     // Read JSON string from file
        session = JsonUtility.FromJson<NumberGuessingSession>(json);  // Convert JSON back to object
        currentState = GameState.Playing;                             // Set state to Playing
        return $"Game loaded! Guess a number between {session.GetMin()} and {session.GetMax()}.";
    }

    // Process player input through the CommandProcessor
    public string ProcessCommand(string input)
    {
        return commandProcessor.Process(input); // Forward input
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

        // If no command entered
        if (parts.Length == 0 || string.IsNullOrEmpty(parts[0]))
            return "Please type a command. Type 'help' for a list of commands.";

        string mainCommand = parts[0]; // The first word is the main command

        switch (mainCommand)
        {
            case "start": // Start a new game
                gameManager.StartNewGame();
                return $"Game started! Guess a number between 1 and 10.";

            case "guess": // Make a guess
                if (gameManager.GetCurrentState() != GameManager.GameState.Playing)
                    return "You must start a game first! Type 'start' to begin.";

                if (parts.Length < 2) // No number entered
                    return "Usage: guess <number>";

                if (int.TryParse(parts[1], out int guess)) // Try to parse number
                    return gameManager.HandleGuess(guess); // Forward to GameManager

                return "Please enter a valid number."; // Invalid input

            case "restart": // Restart the game
                gameManager.StartNewGame();
                return "Game restarted! Guess a number between 1 and 10.";

            case "quit": // End the game
                gameManager.EndGame();
                return "Game ended. Type 'start' to play again.";

            case "help": // Show command list
                return "Commands:\n" +
                       "start - Begin a new game\n" +
                       "guess <number> - Make a guess\n" +
                       "restart - Restart the game\n" +
                       "quit - End the game\n" +
                       "save - Save current game progress\n" +
                       "load - Load saved game\n" +
                       "help - Show this list";

            case "save": // Save game
                return gameManager.SaveGame();

            case "load": // Load game
                return gameManager.LoadGame();

            default: // Unknown command
                return "I don't understand that command. Type 'help' for a list of commands.";
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
        targetNumber = UnityEngine.Random.Range(min, max + 1);
    }

    // Check a guess: 0 = correct, -1 = too low, 1 = too high
    public int CheckGuess(int guess)
    {
        if (guess == targetNumber) return 0;
        else if (guess < targetNumber) return -1;
        else return 1;
    }

    // Expose the target number
    public int GetTargetNumber() => targetNumber;

    // Expose min and max for save/load
    public int GetMin() => min;
    public int GetMax() => max;
}