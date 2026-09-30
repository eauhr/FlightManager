using FlightManager.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using FlightManager.Data;


namespace FlightManager.Controllers
{
    public class HelperController : Controller
    {
        public const string Error = "Error";
        public static List<ErrorViewModel> Errors;
        static HelperController()
        {
            Errors = new List<ErrorViewModel>();
        }
        public static void ClearErrors()
        {
            Errors.Clear();
        }
        public static void AddError(string code, string description, string requiestId = null)
        {
            Errors.Add(new ErrorViewModel(code, description, requiestId));
        }
        public static void AddErrors(IdentityResult result)
        {
            foreach (IdentityError error in result.Errors)
            {
                AddError(error.Code, error.Description);
            }
        }
    }

    public static class HelperControllerExtensions
    {
        public static async Task<User> GetLoggedUser(this UserManager<User> userManager, Controller controller)
        {
            if (string.IsNullOrEmpty(controller.User.Identity.Name))
            {
                throw new InvalidOperationException("User is not logged in.");
            }
            User user = await userManager.FindByNameAsync(controller.User.Identity.Name);

            if (user == null)
                throw new InvalidOperationException(
                    $"No user found with name '{controller.User.Identity.Name}'.");

            return user;
        }
    }
  }
