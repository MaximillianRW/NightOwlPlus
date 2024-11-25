namespace NightOwlPlus.Framework
{
    public class NightOwlPlusAPI
    {

        /// <summary>
        /// Adds an event that triggers after the player has been warped to their pre-collapse position.
        /// </summary>
        /// <param name="ID">The id of the event.</param>
        /// <param name="Action">The code that triggers.</param>
        public static void AddPostWarpEvent(string ID, Func<bool> Action)
        {
            ModEntry.PostWarpCharacter.Add(ID, Action);
        }


        /// <summary>
        /// Removes an event that triggers when the player has been warped to their pre-collapse position.
        /// </summary>
        /// <param name="ID"></param>
        public static void RemovePostWarpEvent(string ID)
        {
            if (ModEntry.PostWarpCharacter.ContainsKey(ID))
            {
                ModEntry.PostWarpCharacter.Remove(ID);
            }
        }

        /// <summary>
        /// Adds an event that triggers when the player has stayed up all night until 6:00 A.M.
        /// </summary>
        /// <param name="ID">The id of the event.</param>
        /// <param name="Action">The code that triggers.</param>
        public static void AddPlayerUpLateEvent(string ID, Func<bool> Action)
        {
            ModEntry.OnPlayerStayingUpLate.Add(ID, Action);
        }

        /// <summary>
        /// Removes an event that triggers when the player has stayed up all night.
        /// </summary>
        /// <param name="ID"></param>
        public static void RemovePlayerUpLateEvent(string ID)
        {
            if (ModEntry.OnPlayerStayingUpLate.ContainsKey(ID))
            {
                ModEntry.OnPlayerStayingUpLate.Remove(ID);
            }
        }


    }
}
