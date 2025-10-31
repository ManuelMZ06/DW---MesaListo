using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MesaListo.Data;
using MesaListo.Models;
using MesaListo.Services;

namespace MesaListo.Controllers
{
    [Authorize]
    public class ReservasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly EmailService _emailService;

        public ReservasController(ApplicationDbContext context, UserManager<IdentityUser> userManager, EmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }

        // GET: Reservas
        public async Task<IActionResult> Index()
        {
            var query = _context.Reservas
                .Include(r => r.Cliente)
                .Include(r => r.Mesa)
                    .ThenInclude(m => m.Restaurante)
                .AsQueryable();

            if (User.IsInRole("Cliente"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                query = query.Where(r => r.ClienteId == userId);
            }
            else if (User.IsInRole("Restaurante"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                query = query.Where(r => r.Mesa.Restaurante.UsuarioId == userId);
            }

            return View(await query.ToListAsync());
        }

        // GET: Reservas/Create
        [Authorize(Roles = "Cliente")]
        public IActionResult Create()
        {
            var mesas = _context.Mesas
                .Include(m => m.Restaurante)
                .Where(m => m.Restaurante != null)
                .ToList();

            ViewData["MesaId"] = new SelectList(mesas, "Id", "DisplayInfo");
            return View();
        }

        // POST: Reservas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> Create(Reserva reserva)
        {
            ModelState.Remove("ClienteId");
            ModelState.Remove("Cliente");

            if (reserva.FechaHora.Kind == DateTimeKind.Unspecified)
            {
                reserva.FechaHora = DateTime.SpecifyKind(reserva.FechaHora, DateTimeKind.Utc);
            }

            if (!await IsMesaAvailable(reserva.MesaId, reserva.FechaHora))
            {
                ModelState.AddModelError("FechaHora", "La mesa no está disponible en este horario.");
            }

            if (ModelState.IsValid)
            {
                // 🔹 Cargar la mesa completa con su restaurante antes de guardar
                var mesa = await _context.Mesas
                    .Include(m => m.Restaurante)
                    .FirstOrDefaultAsync(m => m.Id == reserva.MesaId);

                if (mesa == null)
                {
                    ModelState.AddModelError("MesaId", "La mesa seleccionada no existe.");
                    var mesasList = _context.Mesas.Include(m => m.Restaurante).ToList();
                    ViewData["MesaId"] = new SelectList(mesasList, "Id", "DisplayInfo", reserva.MesaId);
                    return View(reserva);
                }

                // Asociar correctamente
                reserva.Mesa = mesa;
                reserva.ClienteId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                reserva.Estado = "Pendiente";

                try
                {
                    _context.Add(reserva);
                    await _context.SaveChangesAsync();

                    // 🔹 Obtener usuario del restaurante
                    var restauranteUserId = mesa.Restaurante.UsuarioId;
                    var restauranteUser = await _userManager.FindByIdAsync(restauranteUserId);

                    if (restauranteUser != null && !string.IsNullOrEmpty(restauranteUser.Email))
                    {
                        await _emailService.EnviarCorreoAsync(
                            restauranteUser.Email,
                            "Nueva reserva recibida",
                            $"Has recibido una nueva reserva para la mesa {mesa.Codigo} el {reserva.FechaHora:dd/MM/yyyy HH:mm}."
                        );
                    }

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error al guardar la reserva: " + ex.Message);
                }
            }

            var mesas = _context.Mesas.Include(m => m.Restaurante).ToList();
            ViewData["MesaId"] = new SelectList(mesas, "Id", "DisplayInfo", reserva.MesaId);
            return View(reserva);
        }


        // GET: Reservas/Edit/5
        [Authorize(Roles = "Admin,Restaurante")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var reserva = await _context.Reservas
                .Include(r => r.Cliente)
                .Include(r => r.Mesa)
                    .ThenInclude(m => m.Restaurante)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reserva == null) return NotFound();

            // 🔹 Cargar mesas para mostrar la mesa actual
            var mesasQuery = _context.Mesas.AsQueryable();

            if (User.IsInRole("Restaurante"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                mesasQuery = mesasQuery.Where(m => m.Restaurante.UsuarioId == userId);
            }

            ViewData["MesaId"] = new SelectList(await mesasQuery.ToListAsync(), "Id", "Codigo", reserva.MesaId);

            ViewData["Estados"] = new SelectList(new[]
            {
        "Pendiente", "Confirmada", "Cancelada", "Completada"
    }, reserva.Estado);

            return View(reserva);
        }

        // POST: Reservas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Restaurante")]
        public async Task<IActionResult> Edit(int id, Reserva reserva)
        {
            if (id != reserva.Id) return NotFound();

            var reservaDB = await _context.Reservas
                .Include(r => r.Mesa)
                    .ThenInclude(m => m.Restaurante)
                .Include(r => r.Cliente)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservaDB == null) return NotFound();

            // 🔹 Si el usuario es Restaurante → solo puede cambiar el Estado
            if (User.IsInRole("Restaurante"))
            {
                reservaDB.Estado = reserva.Estado;
            }
            else // Admin puede editar todo
            {
                reservaDB.Estado = reserva.Estado;
                reservaDB.FechaHora = reserva.FechaHora;
                reservaDB.MesaId = reserva.MesaId;
                reservaDB.ClienteId = reserva.ClienteId;
            }

            _context.Update(reservaDB);
            await _context.SaveChangesAsync();

            // 🔹 Correos automáticos al cliente
            var cliente = await _userManager.FindByIdAsync(reservaDB.ClienteId);
            if (cliente != null && !string.IsNullOrEmpty(cliente.Email))
            {
                if (reservaDB.Estado == "Confirmada")
                {
                    await _emailService.EnviarCorreoAsync(
                        cliente.Email,
                        "Reserva confirmada",
                        $"Tu reserva en {reservaDB.Mesa.Restaurante.Nombre} ha sido confirmada para el {reservaDB.FechaHora:dd/MM/yyyy HH:mm}."
                    );
                }
                else if (reservaDB.Estado == "Completada")
                {
                    await _emailService.EnviarCorreoAsync(
                        cliente.Email,
                        "¡Califica tu experiencia!",
                        $"Tu reserva en {reservaDB.Mesa.Restaurante.Nombre} ha sido completada. ¡Nos encantaría que califiques tu experiencia en la plataforma!"
                    );
                }
            }

            return RedirectToAction(nameof(Index));
        }


        // GET: Reservas/Delete/5
        [Authorize(Roles = "Admin,Restaurante")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var reserva = await _context.Reservas
                .Include(r => r.Mesa)
                    .ThenInclude(m => m.Restaurante)
                .Include(r => r.Cliente)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reserva == null) return NotFound();

            return View(reserva);
        }

        // POST: Reservas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Restaurante")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var reserva = await _context.Reservas.FindAsync(id);
            if (reserva != null)
            {
                _context.Reservas.Remove(reserva);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // ✅ Método helper: Verificar disponibilidad
        private async Task<bool> IsMesaAvailable(int mesaId, DateTime fechaHora, int? excludeReservaId = null)
        {
            var fechaHoraUtc = fechaHora.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(fechaHora, DateTimeKind.Utc)
                : fechaHora.ToUniversalTime();

            var query = _context.Reservas
                .Where(r => r.MesaId == mesaId && r.FechaHora == fechaHoraUtc && r.Estado != "Cancelada");

            if (excludeReservaId.HasValue)
                query = query.Where(r => r.Id != excludeReservaId.Value);

            return !await query.AnyAsync();
        }
    }
}
