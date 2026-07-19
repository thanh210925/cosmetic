using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using COSMETICC.Models;
using Microsoft.AspNetCore.Http;

namespace COSMETICC.Controllers
{
    public class ChatController : Controller
    {
        private readonly AppDbContext _context;

        public ChatController(AppDbContext context)
        {
            _context = context;
        }

        // Get Chat Token/ConnectionId from cookie, or create one
        private string GetChatConnectionId()
        {
            var cookieKey = "ChatConnectionId";
            if (Request.Cookies.TryGetValue(cookieKey, out string? connId) && !string.IsNullOrEmpty(connId))
            {
                return connId;
            }

            var newConnId = Guid.NewGuid().ToString();
            var option = new CookieOptions
            {
                Expires = DateTime.Now.AddDays(30),
                HttpOnly = false,
                Secure = false // set to true if HTTPS is enforced
            };
            Response.Cookies.Append(cookieKey, newConnId, option);
            return newConnId;
        }

        // POST: /Chat/SendMessage
        [HttpPost]
        public async Task<IActionResult> SendMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return Json(new { success = false, message = "Tin nhắn trống!" });
            }

            var connId = GetChatConnectionId();
            var userIdStr = HttpContext.Session.GetString("UserId");
            int? userId = null;
            if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int id))
            {
                userId = id;
            }

            var chatMsg = new ChatMessage
            {
                Sender = "Customer",
                Message = message.Trim(),
                Timestamp = DateTime.Now,
                UserId = userId,
                ConnectionId = connId
            };

            _context.ChatMessages.Add(chatMsg);
            await _context.SaveChangesAsync();

            return Json(new { success = true, msg = chatMsg });
        }

        // GET: /Chat/GetMessages
        [HttpGet]
        public async Task<IActionResult> GetMessages()
        {
            var connId = GetChatConnectionId();
            var messages = await _context.ChatMessages
                .Where(m => m.ConnectionId == connId)
                .OrderBy(m => m.Timestamp)
                .Select(m => new
                {
                    m.Id,
                    m.Sender,
                    m.Message,
                    Timestamp = m.Timestamp.ToString("HH:mm")
                })
                .ToListAsync();

            return Json(messages);
        }

        // ================= ADMIN CHAT WORKFLOW =================

        // GET: /Chat/AdminIndex
        public IActionResult AdminIndex()
        {
            // Verify if admin is logged in
            var adminId = HttpContext.Session.GetString("AdminId");
            if (string.IsNullOrEmpty(adminId))
            {
                return RedirectToAction("Login", "Admin");
            }
            return View();
        }

        // GET: /Chat/GetActiveChats
        [HttpGet]
        public async Task<IActionResult> GetActiveChats()
        {
            // Verify if admin is logged in
            var adminId = HttpContext.Session.GetString("AdminId");
            if (string.IsNullOrEmpty(adminId))
            {
                return Unauthorized();
            }

            // Get unique connection ids with their last message and user info
            var activeChats = await _context.ChatMessages
                .GroupBy(m => m.ConnectionId)
                .Select(g => new
                {
                    ConnectionId = g.Key,
                    LastMessage = g.OrderByDescending(m => m.Timestamp).FirstOrDefault(),
                    UserId = g.Max(m => m.UserId)
                })
                .ToListAsync();

            // Populate user details if available
            var result = new System.Collections.Generic.List<object>();
            foreach (var chat in activeChats)
            {
                string customerName = "Khách vãng lai";
                if (chat.UserId.HasValue)
                {
                    var user = await _context.Users.FindAsync(chat.UserId.Value);
                    if (user != null)
                    {
                        customerName = user.FullName ?? user.Username ?? user.Email ?? "Khách hàng";
                    }
                }

                result.Add(new
                {
                    chat.ConnectionId,
                    CustomerName = customerName,
                    LastMsgText = chat.LastMessage?.Message ?? "",
                    LastMsgSender = chat.LastMessage?.Sender ?? "",
                    LastMsgTime = chat.LastMessage?.Timestamp.ToString("dd/MM HH:mm") ?? ""
                });
            }

            return Json(result);
        }

        // GET: /Chat/GetAdminMessages
        [HttpGet]
        public async Task<IActionResult> GetAdminMessages(string connectionId)
        {
            var adminId = HttpContext.Session.GetString("AdminId");
            if (string.IsNullOrEmpty(adminId))
            {
                return Unauthorized();
            }

            if (string.IsNullOrEmpty(connectionId))
            {
                return BadRequest("Connection ID is required.");
            }

            var messages = await _context.ChatMessages
                .Where(m => m.ConnectionId == connectionId)
                .OrderBy(m => m.Timestamp)
                .Select(m => new
                {
                    m.Id,
                    m.Sender,
                    m.Message,
                    Timestamp = m.Timestamp.ToString("HH:mm")
                })
                .ToListAsync();

            return Json(messages);
        }

        // POST: /Chat/AdminSendMessage
        [HttpPost]
        public async Task<IActionResult> AdminSendMessage(string connectionId, string message)
        {
            var adminId = HttpContext.Session.GetString("AdminId");
            if (string.IsNullOrEmpty(adminId))
            {
                return Unauthorized();
            }

            if (string.IsNullOrEmpty(connectionId) || string.IsNullOrEmpty(message))
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ!" });
            }

            var chatMsg = new ChatMessage
            {
                Sender = "Staff",
                Message = message.Trim(),
                Timestamp = DateTime.Now,
                ConnectionId = connectionId
            };

            _context.ChatMessages.Add(chatMsg);
            await _context.SaveChangesAsync();

            return Json(new { success = true, msg = chatMsg });
        }
    }
}
