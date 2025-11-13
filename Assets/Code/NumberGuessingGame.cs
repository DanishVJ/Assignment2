using UnityEngine;         // Provides access to Unity’s core classes (like MonoBehaviour, Vector3, Debug, etc.)
using Commodore;           // Allows us to use CommodoreBehavior and other Commodore-specific classes

// NumberGuessingGame handles the number guessing game logic and connects to the Commodore terminal
public class NumberGuessingGame : CommodoreBehavior
{
    private GameManager gameManager;  // Holds the main game logic manager

    // Start() is called once when the script is first run
    void Start()
    {
        // Create a new GameManager instance, which handles all game rules
        gameManager = new GameManager();

        // Begin a new game immediately
        gameManager.StartNewGame();
    }

    // Update() is called every frame
    void Update()
    {
        // Optional: detect if the player presses the Enter key
        // Useful if you want to trigger events per frame (not strictly needed for Commodore input)
        if (Input.GetKeyDown(KeyCode.Return))
        {
            Debug.Log("Enter key pressed! Could trigger additional actions here.");
        }
    }

    // ProcessCommand() is called automatically by Commodore whenever the player types something
    // Commodore expects this function to return a string, which it will display in the terminal
    protected override string ProcessCommand(string command)
    {
        // Forward the input text to the GameManager and return its response
        return gameManager.ProcessCommand(command);
    }
}

// ----------------------------------- GameManager -----------------------------------
// Handles game flow, commands, and manages a NumberGuessingSession
public class GameManager
{
    private CommandProcessor commandProcessor;  // Interprets text commands typed by the player
    private NumberGuessingSession session;      // Holds the current number guessing game session

    // Constructor for GameManager
    public GameManager()
    {
        // Create a CommandProcessor and pass this GameManager to it
        commandProcessor = new CommandProcessor(this);
    }

    // Start a new guessing game
    public void StartNewGame()
    {
        // Create a new NumberGuessingSession with numbers from 1 to 10
        session = new NumberGuessingSession(1, 10);
    }

    // HandleGuess() checks a player's numeric guess against the target number
    public string HandleGuess(int guess)
    {
        // If no session exists, instruct the player to restart
        if (session == null)
            return "No game in progress! Type 'restart' to start a new game.";

        // Check the guess against the session
        int result = session.CheckGuess(guess);

        if (result == 0)
        {
            // Player guessed correctly
            int target = session.GetTargetNumber();  // Get the target number
            StartNewGame();                           // Start a new game automatically
            return $"🎉 Correct! The number was {target}. Starting a new game...";
        }
        else if (result < 0)
        {
            // Guess was too low
            return "Too low! Try again.";
        }
        else
        {
            // Guess was too high
            return "Too high! Try again.";
        }
    }

    // ProcessCommand() forwards text input from Commodore to the CommandProcessor
    public string ProcessCommand(string input)
    {
        return commandProcessor.Process(input);
    }
}

// ----------------------------------- CommandProcessor -----------------------------------
// Interprets player text commands and calls appropriate GameManager functions
public class CommandProcessor
{
    private GameManager gameManager;  // Reference to GameManager to call game logic

    // Constructor receives a GameManager reference
    public CommandProcessor(GameManager manager)
    {
        gameManager = manager;
    }

    // Process() handles the text command typed by the player
    public string Process(string command)
    {
        // Split command into parts (words) to parse arguments
        string[] parts = command.ToLower().Split(' ');

        // If no command typed, prompt the player
        if (parts.Length == 0 || string.IsNullOrEmpty(parts[0]))
            return "Please type a command.";

        string mainCommand = parts[0];  // The first word determines the command type

        switch (mainCommand)
        {
            case "guess":
                // Ensure player typed a number after "guess"
                if (parts.Length < 2)
                    return "Usage: guess <number>";

                // Convert the second word to an integer
                if (int.TryParse(parts[1], out int guess))
                    return gameManager.HandleGuess(guess);  // Forward to GameManager and return response

                // If parsing fails, tell the player
                return "Please enter a valid number.";

            case "restart":
                // Restart the game
                gameManager.StartNewGame();
                return "New game started! Try guessing again.";

            default:
                // Unknown command
                return "I don't understand that command. Try 'guess <number>' or 'restart'.";
        }
    }
}

// ----------------------------------- NumberGuessingSession -----------------------------------
// Manages a single game session, including the target number
public class NumberGuessingSession
{
    private int targetNumber;  // The secret number the player must guess
    private int min;           // Minimum value
    private int max;           // Maximum value

    // Constructor sets the target number randomly between min and max
    public NumberGuessingSession(int min, int max)
    {
        this.min = min;
        this.max = max;
        targetNumber = UnityEngine.Random.Range(min, max + 1); // Random.Range is inclusive min, exclusive max+1
    }

    // CheckGuess() compares the guess with the target number
    // Returns 0 if correct, -1 if too low, 1 if too high
    public int CheckGuess(int guess)
    {
        if (guess == targetNumber)
            return 0;   // correct
        else if (guess < targetNumber)
            return -1;  // too low
        else
            return 1;   // too high
    }

    // GetTargetNumber() exposes the target number
    public int GetTargetNumber()
    {
        return targetNumber;
    }
}