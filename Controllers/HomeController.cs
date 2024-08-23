using System.Data.SqlClient;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using GelirGider.Models;

namespace GelirGider.Controllers;

public class HomeController : Controller
{
    string connectionString =
        "";
    public string TokenUret(int userId)
    {
        var token = Guid.NewGuid().ToString();

        using var connection = new SqlConnection(connectionString);
        var sql = "INSERT INTO pwResetToken (UserId, Token, Created, Used) VALUES (@UserId, @Token, GETDATE(), 0)";
        connection.Execute(sql, new { UserId = userId, Token = token });

        return token;
    }
    public Register GetMail(int id)
    {
        using var connection = new SqlConnection(connectionString);
        var sql = "SELECT * FROM users WHERE Id = @id";
        return connection.QueryFirstOrDefault<Register>(sql, new { Id = id });
    }
    
    public IActionResult Index()
    {
        ViewData["Name"] = HttpContext.Session.GetString("name");
        if (ViewData["Name"] != null)
        {
            return View("DashBoard");
        }
        return View();
    }

    public IActionResult Login()
    {
        return View();
    }

    public IActionResult Register()
    {
        return View();
    }
    
    [HttpPost]
    [Route("/login")]
    public IActionResult Giris(Login model)
    {
        if (!ModelState.IsValid)
        {
            TempData["AuthError"] = "Form eksik.";
            return RedirectToAction("Login");
        }

        model.Pw = Helper.Hash(model.Pw);
        using var connection = new SqlConnection(connectionString);
        var sql = "SELECT * FROM users WHERE Mail = @Mail AND Pw = @Pw";
        var user = connection.QueryFirstOrDefault<Login>(sql, new { model.Mail, model.Pw });

        if (user != null)
        {
            HttpContext.Session.SetInt32("userId", user.Id);
            HttpContext.Session.SetString("name", user.Name);
            ViewData["Name"] = HttpContext.Session.GetString("name");

            ViewBag.Message = "login Başarılı";
            return View("Msg");
        }

        TempData["AuthError"] = "Kullanıcı adı veya şifre hatalı";
        return View("Login");
    }

    [HttpPost]
    [Route("/register")]
    public IActionResult KayitOl(Register model)
    {
        if (!ModelState.IsValid)
        {
            TempData["AuthError"] = "Form eksik veya hatalı.";
            return View("Register");
        }

        if (model.Pw != model.Pwconfirmend)
        {
            TempData["AuthError"] = "Şifreler Uyuşmuyor.";
            return View("Register", model);
        }

        using (var control = new SqlConnection(connectionString))
        {
            var cntrl = "SELECT * FROM users WHERE Mail = @Mail";
            var user = control.QueryFirstOrDefault(cntrl, new { model.Mail });
            if (user != null)
            {
                TempData["AuthError"] = "Bu Mail mevcut!.";
                return View("Register", model);
            }
        }


        model.Created = DateTime.Now;
        model.Pw = Helper.Hash(model.Pw);
        using var connection = new SqlConnection(connectionString);
        var sql =
            "INSERT INTO users (Name, Pw, Mail, Created) VALUES (@Name, @Pw, @Mail, @Created) ";
        var data = new
        {
            model.Name,
            model.Pw,
            model.Mail,
            model.Created,
        };

        var rowAffected = connection.Execute(sql, data);


        ViewBag.Message = "Kayit Başarılı";


        return View("Msg");
    }
    
    [HttpGet]
    [Route("/sifremi-unuttum")]
    public IActionResult PwReset()
    {
        return View();
    }

    [HttpPost]
    public IActionResult PwReset(string email)
    {
        using var connection = new SqlConnection(connectionString);

        var user = connection.QuerySingleOrDefault<Register>(
            "SELECT * FROM users WHERE Mail = @Mail", new { Mail = email });

        if (user == null)
        {
            TempData["AuthError"] = "Bu e-posta adresiyle kayıtlı bir kullanıcı bulunamadı.";
            return View("PwReset");
        }

        return RedirectToAction("PwResetLink", new { userId = user.Id });
    }

    public IActionResult PwResetLink(int userId)
    {
        using var connection = new SqlConnection(connectionString);
        string? userEmail =
            connection.QueryFirstOrDefault<string>("SELECT Mail FROM users WHERE Id = @UserId",new { UserId = userId });

        var token = TokenUret(userId);

        var resetLink = Url.Action("ResetPassword", "Home", new { token }, Request.Scheme);

        using var reader = new StreamReader("wwwroot/mailTemp/pwreset.html");
        var template = reader.ReadToEnd();
        var mailBody = template.Replace("{{Resetlink}}", resetLink);
        Debug.WriteLine("Gönderilen Bağlantı: " + resetLink);

        var client = new SmtpClient("smtp.eu.mailgun.org", 587)
        {
            Credentials = new NetworkCredential(),
            EnableSsl = true
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress("morapp@bildirim.veyselguler.com", "MorApp"),
            Subject = "Şifre Sıfırlama Talebi",
            Body = mailBody,
            IsBodyHtml = true
        };

        mailMessage.To.Add(new MailAddress(userEmail));

        client.Send(mailMessage);

        ViewBag.Message = "Şifre sıfırlama mail olarak iletişmiştir.";
        return View("Msg");
    }

    [HttpGet]
    [Route("/reset-password")]
    public IActionResult ResetPassword(string token)
    {
        using var connection = new SqlConnection(connectionString);
        var resetToken = connection.QuerySingleOrDefault<ResetPwToken>(
            "SELECT * FROM PwResetToken WHERE Token = @Token AND Used = 0", new { Token = token });

        if (resetToken == null)
        {
            ViewBag.Message = "Geçersiz veya kullanılmış token";
            return View("Msg");
        }

        return View(new PwReset { Token = token });
    }

    [HttpPost]
    public IActionResult ResetPassword(PwReset model)
    {
        if (!ModelState.IsValid) return View(model);

        if (model.Pw != model.PwConfirmend)
        {
            TempData["AuthError"] = "Şifreler uyuşmuyor.";
            return View(model);
        }


        using var connection = new SqlConnection(connectionString);
        var resetToken = connection.QueryFirstOrDefault<ResetPwToken>(
            "SELECT * FROM pwResetToken WHERE Token = @Token AND Used = 0", new { model.Token });

        if (resetToken == null)
        {
            ViewBag.Message = "Geçersiz veya kullanılmış token";
            return View("Msg");
        }

        model.Pw = Helper.Hash(model.Pw);

        connection.Execute(
            "UPDATE users SET Pw = @Pw WHERE Id = @UserId",
            new { model.Pw, resetToken.UserId }
        );

        connection.Execute(
            "UPDATE pwResetToken SET Used = 1 WHERE Id = @Id",
            new { resetToken.Id }
        );

        ViewBag.Message = "Şifre Başarılı bir şekilde değiştirildi";
        return View("Msg");
    }

    public IActionResult DashBoard()
    {
        ViewData["Name"] = HttpContext.Session.GetString("name");
        var userId = HttpContext.Session.GetInt32("userId");

        if (userId == null)
        {
            return View("Index");
        }

        var dashBoard = new DashBoard
        {
            CategoriesList = new List<Categories>(), 
            CagsList = new List<Cags>() 
        };
        
        using (var connection = new SqlConnection(connectionString))
        {
            var sql = "SELECT * FROM categories WHERE UserId = @UserId";
            dashBoard.CategoriesList = connection.Query<Categories>(sql, new {UserId = userId}).ToList();
        }
        using (var connection = new SqlConnection(connectionString))
        {
            var sql = "SELECT * FROM cags WHERE UserId = @UserId";
            dashBoard.CategoriesList = connection.Query<Categories>(sql, new {UserId = userId}).ToList();
        }
        
        if (!dashBoard.CategoriesList.Any() && !dashBoard.CagsList.Any())
        {
            return View("EmptyDashBoard"); 
        }
        
        return View(dashBoard);
    }
}

