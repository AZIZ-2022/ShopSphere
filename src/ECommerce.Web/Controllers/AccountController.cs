using System.Security.Claims;
using ECommerce.Application.Cart;
using ECommerce.Application.Orders;
using ECommerce.Infrastructure.Identity;
using ECommerce.Web.Infrastructure;
using ECommerce.Web.Models;
using ECommerce.Web.Models.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Web.Controllers;

[Route("account")]
public sealed class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ICartService _cartService;
    private readonly ICartOwnerAccessor _cartOwner;
    private readonly IAddressService _addresses;
    private readonly IOrderService _orders;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ICartService cartService,
        ICartOwnerAccessor cartOwner,
        IAddressService addresses,
        IOrderService orders,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _cartService = cartService;
        _cartOwner = cartOwner;
        _addresses = addresses;
        _orders = orders;
        _logger = logger;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private async Task MergeGuestCartAsync(ApplicationUser user)
    {
        var anonymousId = _cartOwner.GetAnonymousId();
        if (!string.IsNullOrEmpty(anonymousId))
            await _cartService.MergeGuestCartAsync(anonymousId, user.Id);
    }

    // ---------------------------------------------------------------- Login

    [HttpGet("login")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToLocal(returnUrl);

        this.SetBreadcrumb(new BreadcrumbItem("Log in"));
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        this.SetBreadcrumb(new BreadcrumbItem("Log in"));
        if (!ModelState.IsValid)
            return View(model);

        var existing = await _userManager.FindByEmailAsync(model.Email);
        if (existing is { IsActive: false })
        {
            ModelState.AddModelError(string.Empty, "This account has been deactivated. Please contact support.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {Email} signed in.", model.Email);
            var user = existing ?? await _userManager.FindByEmailAsync(model.Email);
            if (user is not null)
            {
                user.LastLoginAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
                await MergeGuestCartAsync(user);

                if (await _userManager.IsInRoleAsync(user, Roles.Admin))
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }
            return RedirectToLocal(model.ReturnUrl);
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("User {Email} is locked out.", model.Email);
            ModelState.AddModelError(string.Empty, "This account is temporarily locked due to repeated failed attempts. Try again later.");
        }
        else if (result.IsNotAllowed)
        {
            ModelState.AddModelError(string.Empty, "This account is not allowed to sign in.");
        }
        else
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
        }

        return View(model);
    }

    // ------------------------------------------------------------- Register

    [HttpGet("register")]
    [AllowAnonymous]
    public IActionResult Register(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToLocal(returnUrl);

        this.SetBreadcrumb(new BreadcrumbItem("Create account"));
        return View(new RegisterViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        this.SetBreadcrumb(new BreadcrumbItem("Create account"));
        if (!ModelState.IsValid)
            return View(model);

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim(),
            EmailConfirmed = true, // auto-confirm (per project decision)
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var create = await _userManager.CreateAsync(user, model.Password);
        if (!create.Succeeded)
        {
            foreach (var error in create.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        await _userManager.AddToRoleAsync(user, Roles.Customer);
        await MergeGuestCartAsync(user);
        await _signInManager.SignInAsync(user, isPersistent: false);
        _logger.LogInformation("New account created for {Email}.", model.Email);

        TempData["Flash"] = "Welcome to the store! Your account is ready.";
        return RedirectToLocal(model.ReturnUrl);
    }

    // --------------------------------------------------------------- Logout

    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User signed out.");
        return RedirectToAction(nameof(HomeController.Index), "Home");
    }

    [HttpGet("access-denied")]
    public IActionResult AccessDenied()
    {
        this.SetBreadcrumb(new BreadcrumbItem("Access denied"));
        return View();
    }

    // -------------------------------------------------------------- Profile

    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        this.SetBreadcrumb(new BreadcrumbItem("My Account", Url.Action(nameof(Profile))), new BreadcrumbItem("Profile"));

        ViewBag.ChangePassword = new ChangePasswordViewModel();
        return View(new ProfileViewModel
        {
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            MemberSince = user.CreatedAt
        });
    }

    [HttpPost("profile")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        this.SetBreadcrumb(new BreadcrumbItem("My Account", Url.Action(nameof(Profile))), new BreadcrumbItem("Profile"));
        ViewBag.ChangePassword = new ChangePasswordViewModel();

        model.Email = user.Email ?? string.Empty;
        model.MemberSince = user.CreatedAt;

        if (!ModelState.IsValid)
            return View(model);

        user.FullName = model.FullName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();

        var update = await _userManager.UpdateAsync(user);
        if (!update.Succeeded)
        {
            foreach (var error in update.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        await _signInManager.RefreshSignInAsync(user);
        TempData["Flash"] = "Your profile has been updated.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpPost("change-password")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        this.SetBreadcrumb(new BreadcrumbItem("My Account", Url.Action(nameof(Profile))), new BreadcrumbItem("Profile"));

        if (!ModelState.IsValid)
        {
            ViewBag.ChangePassword = model;
            return View(nameof(Profile), new ProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                MemberSince = user.CreatedAt
            });
        }

        var change = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!change.Succeeded)
        {
            foreach (var error in change.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            ViewBag.ChangePassword = model;
            return View(nameof(Profile), new ProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                MemberSince = user.CreatedAt
            });
        }

        await _signInManager.RefreshSignInAsync(user);
        TempData["Flash"] = "Your password has been changed.";
        return RedirectToAction(nameof(Profile));
    }

    // -------------------------------------------------------------- Orders

    [HttpGet("orders")]
    [Authorize]
    public async Task<IActionResult> Orders(CancellationToken ct)
    {
        this.SetBreadcrumb(new BreadcrumbItem("My Account", Url.Action(nameof(Profile))), new BreadcrumbItem("Orders"));
        var orders = await _orders.ListForCustomerAsync(CurrentUserId, ct);
        return View(orders);
    }

    [HttpGet("orders/{orderNumber}")]
    [Authorize]
    public async Task<IActionResult> OrderDetail(string orderNumber, CancellationToken ct)
    {
        var order = await _orders.GetForCustomerAsync(CurrentUserId, orderNumber, ct);
        if (order is null) return NotFound();

        this.SetBreadcrumb(
            new BreadcrumbItem("My Account", Url.Action(nameof(Profile))),
            new BreadcrumbItem("Orders", Url.Action(nameof(Orders))),
            new BreadcrumbItem(order.OrderNumber));
        return View(order);
    }

    [HttpPost("orders/{orderNumber}/cancel")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelOrder(string orderNumber, string? reason, CancellationToken ct)
    {
        var result = await _orders.CancelAsync(CurrentUserId, orderNumber, reason, ct);
        TempData[result.Succeeded ? "Flash" : "FlashError"] = result.Succeeded ? "Your order has been cancelled." : result.Error;
        return RedirectToAction(nameof(OrderDetail), new { orderNumber });
    }

    [HttpPost("orders/{orderNumber}/return")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestReturn(string orderNumber, string? reason, CancellationToken ct)
    {
        var result = await _orders.RequestReturnAsync(CurrentUserId, orderNumber, reason, ct);
        TempData[result.Succeeded ? "Flash" : "FlashError"] = result.Succeeded ? "Your return request has been submitted." : result.Error;
        return RedirectToAction(nameof(OrderDetail), new { orderNumber });
    }

    // ----------------------------------------------------------- Addresses

    [HttpGet("addresses")]
    [Authorize]
    public async Task<IActionResult> Addresses(CancellationToken ct)
    {
        this.SetBreadcrumb(new BreadcrumbItem("My Account", Url.Action(nameof(Profile))), new BreadcrumbItem("Addresses"));
        return View(await _addresses.ListAsync(CurrentUserId, ct));
    }

    [HttpGet("addresses/new")]
    [Authorize]
    public IActionResult AddAddress()
    {
        this.SetBreadcrumb(new BreadcrumbItem("My Account", Url.Action(nameof(Profile))),
            new BreadcrumbItem("Addresses", Url.Action(nameof(Addresses))), new BreadcrumbItem("New address"));
        ViewData["Title"] = "New address";
        return View("AddressForm", new AddressEditModel { Country = "Bangladesh" });
    }

    [HttpPost("addresses/new")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAddress(AddressEditModel model, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            var result = await _addresses.CreateAsync(CurrentUserId, model, ct);
            if (result.Succeeded)
            {
                TempData["Flash"] = "Address saved.";
                return RedirectToAction(nameof(Addresses));
            }
            ModelState.AddModelError(string.Empty, result.Error!);
        }
        ViewData["Title"] = "New address";
        return View("AddressForm", model);
    }

    [HttpGet("addresses/{id:int}/edit")]
    [Authorize]
    public async Task<IActionResult> EditAddress(int id, CancellationToken ct)
    {
        var address = await _addresses.GetAsync(CurrentUserId, id, ct);
        if (address is null) return NotFound();

        this.SetBreadcrumb(new BreadcrumbItem("My Account", Url.Action(nameof(Profile))),
            new BreadcrumbItem("Addresses", Url.Action(nameof(Addresses))), new BreadcrumbItem("Edit address"));
        ViewData["Title"] = "Edit address";
        return View("AddressForm", new AddressEditModel
        {
            Id = address.Id, FullName = address.FullName, Phone = address.Phone,
            Line1 = address.Line1, Line2 = address.Line2, City = address.City, State = address.State,
            PostalCode = address.PostalCode, Country = address.Country, IsDefault = address.IsDefault
        });
    }

    [HttpPost("addresses/{id:int}/edit")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAddress(int id, AddressEditModel model, CancellationToken ct)
    {
        model.Id = id;
        if (ModelState.IsValid)
        {
            var result = await _addresses.UpdateAsync(CurrentUserId, model, ct);
            if (result.Succeeded)
            {
                TempData["Flash"] = "Address updated.";
                return RedirectToAction(nameof(Addresses));
            }
            ModelState.AddModelError(string.Empty, result.Error!);
        }
        ViewData["Title"] = "Edit address";
        return View("AddressForm", model);
    }

    [HttpPost("addresses/{id:int}/delete")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAddress(int id, CancellationToken ct)
    {
        var result = await _addresses.DeleteAsync(CurrentUserId, id, ct);
        TempData[result.Succeeded ? "Flash" : "FlashError"] = result.Succeeded ? "Address removed." : result.Error;
        return RedirectToAction(nameof(Addresses));
    }

    [HttpPost("addresses/{id:int}/default")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDefaultAddress(int id, CancellationToken ct)
    {
        await _addresses.SetDefaultAsync(CurrentUserId, id, ct);
        TempData["Flash"] = "Default address updated.";
        return RedirectToAction(nameof(Addresses));
    }

    // --------------------------------------------------------------- helpers

    private IActionResult RedirectToLocal(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(HomeController.Index), "Home");
}
