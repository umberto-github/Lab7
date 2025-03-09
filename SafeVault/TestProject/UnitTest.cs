using NUnit.Framework;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using IdentityTest.Data;
using IdentityTest.Controllers;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace TestProject
{
    [TestFixture]
    public class UnitTest
    {
        private UserManager<IdentityUser> _userManager;
        private SignInManager<IdentityUser> _signInManager;
        private ApplicationDbContext _dbContext;
        private AccountController _accountController;
        private IServiceScope _scope;

        [SetUp]
        public async Task Setup()
        {
            // Configurazione del database in-memory
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase("InMemoryUserAuthApp"));

            // Configurazione di Identity
            serviceCollection.AddIdentity<IdentityUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = false)
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            // Aggiungi i servizi di logging
            serviceCollection.AddLogging();

            var serviceProvider = serviceCollection.BuildServiceProvider();
            _scope = serviceProvider.CreateScope();

            _dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            _userManager = _scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            _signInManager = _scope.ServiceProvider.GetRequiredService<SignInManager<IdentityUser>>();

            // Creazione del controller
            _accountController = new AccountController(_userManager, _signInManager);

            // Creazione di ruoli e utente admin (come nel tuo Startup)
            var roleManager = _scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            await CreateRolesAndAdminUser(roleManager, _userManager);
        }

        [Test]
        public async Task Register_ValidData_ShouldSucceed()
        {
            // Arrange
            var model = new IdentityTest.Models.RegisterViewModel
            {
                Username = "testuser",
                Email = "test@example.com",
                Password = "Password123!"
            };

            // Act
            var result = await _accountController.Register(model) as RedirectToActionResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);
            Assert.AreEqual("Home", result.ControllerName);
        }

        [Test]
        public async Task Register_XssAttack_ShouldFail()
        {
            // Arrange
            var model = new IdentityTest.Models.RegisterViewModel
            {
                Username = "<script>alert('XSS');</script>",
                Email = "xss@example.com",
                Password = "Password123!"
            };

            // Act
            var result = await _accountController.Register(model) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(_accountController.ModelState.IsValid);

        }

        [Test]
        public async Task Register_SqlInjectionAttack_ShouldFail()
        {
            // Arrange
            var model = new IdentityTest.Models.RegisterViewModel
            {
                Username = "user' OR '1'='1",
                Email = "sql@example.com",
                Password = "Password123!"
            };

            // Act
            var result = await _accountController.Register(model) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(_accountController.ModelState.IsValid);
        }

        [Test]
        public async Task Login_ValidCredentials_ShouldSucceed()
        {
            // Crea un contesto HTTP reale
            var httpContext = new DefaultHttpContext();

            // Assegna il contesto HTTP al controller
            _accountController.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            var model = new IdentityTest.Models.LoginViewModel
            {
                Email = "admin@admin.com",
                Password = "Admin@123",
                RememberMe = false
            };

            // Simula la chiamata al metodo Login
            var result = await _accountController.Login(model) as OkResult;

            // Verifica il risultato
            Assert.IsNotNull(result);
            Assert.IsTrue(_accountController.ModelState.IsValid);
        }


        [Test]
        public async Task Login_InvalidCredentials_ShouldFail()
        {
            // Arrange
            var model = new IdentityTest.Models.LoginViewModel
            {
                Email = "adminq123@admin.com",
                Password = "WrongPassword",
                RememberMe = false
            };

            // Act
            var result = await _accountController.Login(model) as BadRequestObjectResult;

            // Assert
            Assert.IsNull(result);
            Assert.AreNotEqual(400, result.StatusCode);
            Assert.AreNotEqual("Invalid login attempt.", result.Value);
        }

        [Test]
        public async Task Login_SqlInjectionAttack_ShouldFail()
        {
            // Arrange
            var model = new IdentityTest.Models.LoginViewModel
            {
                Email = "user' OR '1'='1",
                Password = "password",
                RememberMe = false
            };

            // Act
            var result = await _accountController.Login(model) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(_accountController.ModelState.IsValid);
        }

        [Test]
        public async Task Login_XssAttack_ShouldFail()
        {
            // Arrange
            var model = new IdentityTest.Models.LoginViewModel
            {
                Email = "<script>alert('XSS');</script>",
                Password = "password",
                RememberMe = false
            };

            // Act
            var result = await _accountController.Login(model) as ViewResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(_accountController.ModelState.IsValid);
        }




        private async Task CreateRolesAndAdminUser(RoleManager<IdentityRole> roleManager, UserManager<IdentityUser> userManager)
        {
            string[] roleNames = { "Admin" };
            IdentityResult roleResult;

            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            var adminUser = new IdentityUser
            {
                UserName = "admin@admin.com",
                Email = "admin@admin.com"
            };

            string adminPassword = "Admin@123";
            var _adminUser = await userManager.FindByEmailAsync("admin@admin.com");

            if (_adminUser == null)
            {
                var createAdminUser = await userManager.CreateAsync(adminUser, adminPassword);
                if (createAdminUser.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }

        [TearDown]
        public async Task TearDown()
        {
            _userManager?.Dispose();
            _accountController?.Dispose();
            _dbContext?.Dispose();
            _scope?.Dispose();
        }


        //executed one time at the end of tests
        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            if (_signInManager != null)
             {
                 //await _signInManager.SignOutAsync();
             }
        }
    }
}