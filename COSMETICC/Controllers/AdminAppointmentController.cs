using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using COSMETICC.Models;

namespace COSMETICC.Controllers
{
    public class AdminAppointmentController : AdminBaseController
    {
        private readonly AppDbContext _context;

        public AdminAppointmentController(AppDbContext context)
        {
            _context = context;
        }

        // GET: AdminAppointment
        public async Task<IActionResult> Index(string? status, string? search, DateTime? date)
        {
            var query = _context.Appointments.AsQueryable();

            if (!string.IsNullOrEmpty(status) && status != "Tất cả")
            {
                query = query.Where(a => a.Status == status);
            }

            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim();
                query = query.Where(a => a.FullName.Contains(search) || 
                                         a.PhoneNumber.Contains(search) || 
                                         (a.Email != null && a.Email.Contains(search)) ||
                                         (a.DoctorName != null && a.DoctorName.Contains(search)));
            }

            if (date.HasValue)
            {
                query = query.Where(a => a.AppointmentDate.Date == date.Value.Date);
            }

            var appointments = await query
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            ViewBag.CurrentStatus = status ?? "Tất cả";
            ViewBag.SearchQuery = search ?? "";
            ViewBag.FilterDate = date?.ToString("yyyy-MM-dd") ?? "";

            // Stats
            ViewBag.TotalCount = await _context.Appointments.CountAsync();
            ViewBag.PendingCount = await _context.Appointments.CountAsync(a => a.Status == "Chờ xác nhận");
            ViewBag.ConfirmedCount = await _context.Appointments.CountAsync(a => a.Status == "Đã xác nhận");
            ViewBag.CompletedCount = await _context.Appointments.CountAsync(a => a.Status == "Đã khám");
            ViewBag.TodayCount = await _context.Appointments.CountAsync(a => a.AppointmentDate.Date == DateTime.Today);

            return View(appointments);
        }

        // POST: AdminAppointment/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy lịch hẹn!";
                return RedirectToAction(nameof(Index));
            }

            appointment.Status = status;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã cập nhật trạng thái lịch hẹn #{id} thành \"{status}\"!";
            return RedirectToAction(nameof(Index));
        }

        // POST: AdminAppointment/UpdateDoctorAndNotes
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateDoctorAndNotes(int id, string? doctorName, string? notes)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy lịch hẹn!";
                return RedirectToAction(nameof(Index));
            }

            if (!string.IsNullOrWhiteSpace(doctorName))
            {
                appointment.DoctorName = doctorName;
            }
            appointment.Notes = notes;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã cập nhật thông tin Bác sĩ / Ghi chú cho lịch hẹn #APT-{appointment.Id:D5}!";
            return RedirectToAction(nameof(Index));
        }

        // POST: AdminAppointment/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã xóa lịch hẹn #{id}!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
