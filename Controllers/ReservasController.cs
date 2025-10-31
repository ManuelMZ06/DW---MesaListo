using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MesaListo.Data;
using MesaListo.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace MesaListo.Controllers
{
    [Authorize]
    public class ReservasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReservasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Reservas
        public async Task<IActionResult> Index()
        {
            var query = _context.Reservas
                .Include(r => r.Cliente)
                .Include(r => r.Mesa)
                    .ThenInclude(m => m.Restaurante)
                .AsQueryable();

            // Filtros por rol
            if (User.IsInRole("Cliente"))
            {
                // Clientes solo ven SUS reservas
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                query = query.Where(r => r.ClienteId == userId);
            }
            else if (User.IsInRole("Restaurante"))
            {
                // Restaurantes ven reservas de SUS mesas
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                query = query.Where(r => r.Mesa.Restaurante.UsuarioId == userId);
            }
            // Admin ve TODAS las reservas (sin filtro adicional)

            return View(await query.ToListAsync());
        }

        // GET: Reservas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reserva = await _context.Reservas
                .Include(r => r.Cliente)
                .Include(r => r.Mesa)
                    .ThenInclude(m => m.Restaurante)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (reserva == null)
            {
                return NotFound();
            }

            // Verificar permisos
            if (!CanAccessReserva(reserva))
            {
                return Forbid();
            }

            return View(reserva);
        }

        // GET: Reservas/Create
        [Authorize(Roles = "Cliente")] // SOLO clientes pueden crear reservas
        public IActionResult Create()
        {
            // Clientes pueden ver todas las mesas disponibles
            var mesasQuery = _context.Mesas
                .Include(m => m.Restaurante)
                .Where(m => m.Restaurante != null) // Solo mesas con restaurante
                .AsQueryable();

            ViewData["MesaId"] = new SelectList(mesasQuery, "Id", "DisplayInfo");
            return View();
        }

        // POST: Reservas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Cliente")] // SOLO clientes pueden crear reservas
        public async Task<IActionResult> Create(Reserva reserva)
        {
            // SOLO Clientes pueden crear reservas - validación adicional
            if (!User.IsInRole("Cliente"))
            {
                return Forbid();
            }

            // Remover errores de ClienteId ya que lo asignaremos automáticamente
            ModelState.Remove("ClienteId");
            ModelState.Remove("Cliente");

            // Convertir FechaHora a UTC si es necesario
            if (reserva.FechaHora.Kind == DateTimeKind.Unspecified)
            {
                reserva.FechaHora = DateTime.SpecifyKind(reserva.FechaHora, DateTimeKind.Utc);
            }

            // Validar disponibilidad
            if (!await IsMesaAvailable(reserva.MesaId, reserva.FechaHora))
            {
                ModelState.AddModelError("FechaHora", "La mesa no está disponible en este horario.");
            }

            if (ModelState.IsValid)
            {
                // Asignar cliente automáticamente
                reserva.ClienteId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Estado inicial siempre "Pendiente" para nuevas reservas de clientes
                reserva.Estado = "Pendiente";

                try
                {
                    _context.Add(reserva);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error al guardar la reserva: " + ex.Message);
                }
            }

            // Recargar ViewData si hay error
            var mesasQuery = _context.Mesas
                .Include(m => m.Restaurante)
                .Where(m => m.Restaurante != null)
                .AsQueryable();

            ViewData["MesaId"] = new SelectList(mesasQuery, "Id", "DisplayInfo", reserva.MesaId);
            return View(reserva);
        }

        // GET: Reservas/Edit/5
        [Authorize(Roles = "Admin,Restaurante")] // Clientes NO pueden editar reservas
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reserva = await _context.Reservas
                .Include(r => r.Cliente)
                .Include(r => r.Mesa)
                    .ThenInclude(m => m.Restaurante)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reserva == null)
            {
                return NotFound();
            }

            // Verificar permisos
            if (!CanAccessReserva(reserva))
            {
                return Forbid();
            }

            await LoadEditViewData(reserva);
            return View(reserva);
        }

        // POST: Reservas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Restaurante")] // Clientes NO pueden editar reservas
        public async Task<IActionResult> Edit(int id, [Bind("Id,FechaHora,MesaId,ClienteId,Estado")] Reserva reserva)
        {
            if (id != reserva.Id)
            {
                return NotFound();
            }

            // Obtener reserva existente
            var existingReserva = await _context.Reservas
                .Include(r => r.Mesa)
                .ThenInclude(m => m.Restaurante)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (existingReserva == null || !CanAccessReserva(existingReserva))
            {
                return Forbid();
            }

            // Si es Restaurante, solo permitir cambiar el Estado
            if (User.IsInRole("Restaurante"))
            {
                // Mantener los valores originales excepto el Estado
                existingReserva.Estado = reserva.Estado;

                // Remover errores de validación para campos que no se están editando
                ModelState.Remove("FechaHora");
                ModelState.Remove("MesaId");
                ModelState.Remove("ClienteId");

                if (ModelState.IsValid)
                {
                    try
                    {
                        _context.Update(existingReserva);
                        await _context.SaveChangesAsync();
                        return RedirectToAction(nameof(Index));
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!ReservaExists(reserva.Id))
                        {
                            return NotFound();
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }
            else // Admin puede editar todo
            {
                // Validar disponibilidad (excluyendo esta reserva)
                if (!await IsMesaAvailable(reserva.MesaId, reserva.FechaHora, id))
                {
                    ModelState.AddModelError("FechaHora", "La mesa no está disponible en este horario");
                }

                if (ModelState.IsValid)
                {
                    try
                    {
                        _context.Update(reserva);
                        await _context.SaveChangesAsync();
                        return RedirectToAction(nameof(Index));
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!ReservaExists(reserva.Id))
                        {
                            return NotFound();
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }

            // Recargar ViewData si hay error
            await LoadEditViewData(reserva);
            return View(reserva);
        }

        // GET: Reservas/Delete/5
        [Authorize(Roles = "Admin,Restaurante")] // Clientes NO pueden eliminar reservas
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reserva = await _context.Reservas
                .Include(r => r.Cliente)
                .Include(r => r.Mesa)
                    .ThenInclude(m => m.Restaurante)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (reserva == null)
            {
                return NotFound();
            }

            // Verificar permisos
            if (!CanAccessReserva(reserva))
            {
                return Forbid();
            }

            return View(reserva);
        }

        // POST: Reservas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Restaurante")] // Clientes NO pueden eliminar reservas
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var reserva = await _context.Reservas
                .Include(r => r.Mesa)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reserva != null)
            {
                // Verificar permisos
                if (!CanAccessReserva(reserva))
                {
                    return Forbid();
                }

                _context.Reservas.Remove(reserva);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // Método helper para validar disponibilidad
        private async Task<bool> IsMesaAvailable(int mesaId, DateTime fechaHora, int? excludeReservaId = null)
        {
            // Convertir a UTC para PostgreSQL
            var fechaHoraUtc = fechaHora.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(fechaHora, DateTimeKind.Utc)
                : fechaHora.ToUniversalTime();

            var query = _context.Reservas
                .Where(r => r.MesaId == mesaId
                         && r.FechaHora == fechaHoraUtc
                         && r.Estado != "Cancelada");

            if (excludeReservaId.HasValue)
            {
                query = query.Where(r => r.Id != excludeReservaId.Value);
            }

            return !await query.AnyAsync();
        }

        // Método helper para verificar permisos - VERSIÓN MEJORADA
        private bool CanAccessReserva(Reserva reserva)
        {
            if (reserva == null) return false;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return false;

            if (User.IsInRole("Admin")) return true;

            if (User.IsInRole("Cliente") && reserva.ClienteId == userId) return true;

            if (User.IsInRole("Restaurante"))
            {
                // Cargar explícitamente la relación si no está cargada
                if (reserva.Mesa?.Restaurante == null)
                {
                    reserva = _context.Reservas
                        .Include(r => r.Mesa)
                        .ThenInclude(m => m.Restaurante)
                        .FirstOrDefault(r => r.Id == reserva.Id);
                }

                return reserva?.Mesa?.Restaurante?.UsuarioId == userId;
            }

            return false;
        }

        // Método helper para cargar ViewData en Edit
        private async Task LoadEditViewData(Reserva reserva)
        {
            var mesasQuery = _context.Mesas.AsQueryable();
            if (User.IsInRole("Restaurante"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                mesasQuery = mesasQuery.Where(m => m.Restaurante.UsuarioId == userId);
            }

            ViewData["MesaId"] = new SelectList(await mesasQuery.ToListAsync(), "Id", "Codigo", reserva.MesaId);
            ViewData["Estados"] = new SelectList(new[]
            {
                new { Value = "Pendiente", Text = "Pendiente" },
                new { Value = "Confirmada", Text = "Confirmada" },
                new { Value = "Cancelada", Text = "Cancelada" },
                new { Value = "Completada", Text = "Completada" }
            }, "Value", "Text", reserva.Estado);
        }

        private bool ReservaExists(int id)
        {
            return _context.Reservas.Any(e => e.Id == id);
        }
    }
}