using BuyOldBike_DAL.Entities;
using System;

namespace BuyOldBike_Presentation.State
{
    public static class AppSession
    {
        public static event EventHandler? CurrentUserChanged;

        public static User? CurrentUser { get; private set; }

        public static bool IsAuthenticated => CurrentUser != null;

        public static void SetCurrentUser(User user)
        {
            CurrentUser = user;
            CurrentUserChanged?.Invoke(null, EventArgs.Empty);
        }

        public static void Clear()
        {
            CurrentUser = null;
            CurrentUserChanged?.Invoke(null, EventArgs.Empty);
        }
    }
}
