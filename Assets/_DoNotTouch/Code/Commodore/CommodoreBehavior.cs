using UnityEngine;

namespace Commodore
{
    /// <summary>
    /// Base class for student implementations.
    /// Students inherit from this and override ProcessCommand to create their game logic.
    /// </summary>
    public abstract class CommodoreBehavior : MonoBehaviour
    {
        /// <summary>
        /// Override this method to process commands from the terminal.
        /// </summary>
        /// <param name="command">The command string entered by the user (in uppercase)</param>
        /// <returns>The response to display in the terminal</returns>
        protected abstract string ProcessCommand(string command);

        /// <summary>
        /// Called by CommodoreTerminal - students should not call this directly.
        /// </summary>
        public string HandleCommand(string command)
        {
            return ProcessCommand(command);
        }
    }
}
