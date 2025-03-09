using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityTest.Controllers
{
    //[Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        /*public IActionResult Index()
        {
            return View("Administrator");
        }*/

        public IActionResult Index()
        {
            // Verifica se l'utente è autenticato
            if (!User.Identity.IsAuthenticated)
            {
                // Reindirizza alla pagina di login
                return RedirectToAction("Login", "Account");
            }

            // Verifica se l'utente ha il ruolo "Admin"
            if (!User.IsInRole("Admin"))
            {
                // Reindirizza alla pagina di accesso negato (o alla pagina di login)
                return RedirectToAction("AccessDenied", "Account");
            }

            // Se l'utente è autenticato e ha il ruolo "Admin", restituisci la vista
            return View("Administrator");
        }
    }
}
