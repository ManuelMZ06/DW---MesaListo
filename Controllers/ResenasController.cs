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
    public class ResenasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ResenasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Resenas
        public async Task<IActionResult> Index()
        {
            var query = _context.Resenas
                .Include(r => r.Cliente)
                .Include(r => r.Reserva)
                    .ThenInclude(res => res.Mesa)
                        .ThenInclude(m => m.Restaurante)
                .AsQueryable();

            // Filtros por rol
            if (User.IsInRole("Cliente"))
            {
                // Clientes solo ven SUS reseñas
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                query = query.Where(r => r.ClienteId == userId);
            }
            else if (User.IsInRole("Restaurante"))
            {
                // Restaurantes ven reseñas de SUS restaurantes
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                query = query.Where(r => r.Reserva.Mesa.Restaurante.UsuarioId == userId);
            }
            // Admin ve TODAS las reseñas (sin filtro adicional)

            return View(await query.ToListAsync());
        }

        // GET: Resenas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var resena = await _context.Resenas
                .Include(r => r.Cliente)
                .Include(r => r.Reserva)
                    .ThenInclude(res => res.Mesa)
                        .ThenInclude(m => m.Restaurante)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (resena == null)
            {
                return NotFound();
            }

            // Verificar permisos
            if (!CanAccessResena(resena))
            {
                return Forbid();
            }

            return View(resena);
        }

        // GET: Resenas/Create
        [Authorize(Roles = "Cliente")]
        public IActionResult Create()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Clientes solo pueden crear reseñas de SUS reservas completadas
            var reservasCompletadas = _context.Reservas
                .Where(r => r.ClienteId == userId && r.Estado == "Completada")
                .Include(r => r.Mesa)
                    .ThenInclude(m => m.Restaurante)
                .Select(r => new {
                    r.Id,
                    DisplayText = $"Reserva #{r.Id} - {r.Mesa.Restaurante.Nombre} - {r.FechaHora:dd/MM/yyyy HH:mm}"
                });

            ViewData["ReservaId"] = new SelectList(reservasCompletadas, "Id", "DisplayText");

            // ClienteId se asigna automáticamente, no mostramos dropdown
            ViewData["ClienteId"] = new SelectList(_context.Users.Where(u => u.Id == userId), "Id", "Email", userId);

            return View();
        }

        // POST: Resenas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Cliente")]
        public async Task<IActionResult> Create([Bind("Id,Puntuacion,Comentario,ReservaId")] Resena resena) // REMOVER ClienteId y FechaCreacion del Bind
        {
            // Remover propiedades que asignaremos automáticamente
            ModelState.Remove("ClienteId");
            ModelState.Remove("FechaCreacion");
            ModelState.Remove("Cliente");
            ModelState.Remove("Reserva");

            // Validar que la reserva pertenece al cliente y está completada
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var reservaValida = await _context.Reservas
                .AnyAsync(r => r.Id == resena.ReservaId &&
                              r.ClienteId == userId &&
                              r.Estado == "Completada");

            if (!reservaValida)
            {
                ModelState.AddModelError("ReservaId", "Solo puedes crear reseñas para tus reservas completadas.");
            }

            // Validar que no existe ya una reseña para esta reserva
            var reseñaExistente = await _context.Resenas
                .AnyAsync(r => r.ReservaId == resena.ReservaId);

            if (reseñaExistente)
            {
                ModelState.AddModelError("ReservaId", "Ya has creado una reseña para esta reserva.");
            }

            if (ModelState.IsValid)
            {
                // Asignar propiedades automáticamente
                resena.ClienteId = userId;
                resena.FechaCreacion = DateTime.UtcNow;

                try
                {
                    _context.Add(resena);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error al guardar la reseña: " + ex.Message);
                }
            }

            // Recargar ViewData si hay error
            var reservasCompletadas = _context.Reservas
                .Where(r => r.ClienteId == userId && r.Estado == "Completada")
                .Include(r => r.Mesa)
                    .ThenInclude(m => m.Restaurante)
                .Select(r => new {
                    r.Id,
                    DisplayText = $"Reserva #{r.Id} - {r.Mesa.Restaurante.Nombre} - {r.FechaHora:dd/MM/yyyy HH:mm}"
                });

            ViewData["ReservaId"] = new SelectList(reservasCompletadas, "Id", "DisplayText", resena.ReservaId);
            ViewData["ClienteId"] = new SelectList(_context.Users.Where(u => u.Id == userId), "Id", "Email", userId);

            return View(resena);
        }

        // GET: Resenas/Edit/5
        [Authorize(Roles = "Admin,Cliente")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var resena = await _context.Resenas
                .Include(r => r.Cliente)
                .Include(r => r.Reserva)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (resena == null)
            {
                return NotFound();
            }

            // Verificar permisos
            if (!CanAccessResena(resena))
            {
                return Forbid();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (User.IsInRole("Cliente"))
            {
                // Clientes solo pueden editar sus propias reseñas
                ViewData["ReservaId"] = new SelectList(
                    _context.Reservas.Where(r => r.ClienteId == userId && r.Estado == "Completada"),
                    "Id", "Id", resena.ReservaId);
            }
            else
            {
                // Admin puede ver todas las reservas
                ViewData["ReservaId"] = new SelectList(_context.Reservas, "Id", "Id", resena.ReservaId);
            }

            ViewData["ClienteId"] = new SelectList(_context.Users, "Id", "Email", resena.ClienteId);
            return View(resena);
        }

        // POST: Resenas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Cliente")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Puntuacion,Comentario,ReservaId,ClienteId")] Resena resena) // REMOVER FechaCreacion del Bind
        {
            if (id != resena.Id)
            {
                return NotFound();
            }

            // Obtener reseña existente para validar permisos
            var existingResena = await _context.Resenas
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id);

            if (existingResena == null || !CanAccessResena(existingResena))
            {
                return Forbid();
            }

            // Remover FechaCreacion del ModelState
            ModelState.Remove("FechaCreacion");

            if (ModelState.IsValid)
            {
                try
                {
                    // Mantener la FechaCreacion original
                    resena.FechaCreacion = existingResena.FechaCreacion;

                    _context.Update(resena);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ResenaExists(resena.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            // Recargar ViewData si hay error
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (User.IsInRole("Cliente"))
            {
                ViewData["ReservaId"] = new SelectList(
                    _context.Reservas.Where(r => r.ClienteId == userId && r.Estado == "Completada"),
                    "Id", "Id", resena.ReservaId);
            }
            else
            {
                ViewData["ReservaId"] = new SelectList(_context.Reservas, "Id", "Id", resena.ReservaId);
            }

            ViewData["ClienteId"] = new SelectList(_context.Users, "Id", "Email", resena.ClienteId);
            return View(resena);
        }

        // GET: Resenas/Delete/5
        [Authorize(Roles = "Admin,Cliente")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var resena = await _context.Resenas
                .Include(r => r.Cliente)
                .Include(r => r.Reserva)
                    .ThenInclude(res => res.Mesa)
                        .ThenInclude(m => m.Restaurante)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (resena == null)
            {
                return NotFound();
            }

            // Verificar permisos
            if (!CanAccessResena(resena))
            {
                return Forbid();
            }

            return View(resena);
        }

        // POST: Resenas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Cliente")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var resena = await _context.Resenas
                .Include(r => r.Reserva)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (resena != null)
            {
                // Verificar permisos
                if (!CanAccessResena(resena))
                {
                    return Forbid();
                }

                _context.Resenas.Remove(resena);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // Método helper para verificar permisos
        private bool CanAccessResena(Resena resena)
        {
            if (resena == null) return false;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return false;

            if (User.IsInRole("Admin")) return true;

            if (User.IsInRole("Cliente") && resena.ClienteId == userId) return true;

            if (User.IsInRole("Restaurante"))
            {
                // Cargar explícitamente la relación si no está cargada
                if (resena.Reserva?.Mesa?.Restaurante == null)
                {
                    resena = _context.Resenas
                        .Include(r => r.Reserva)
                        .ThenInclude(res => res.Mesa)
                        .ThenInclude(m => m.Restaurante)
                        .FirstOrDefault(r => r.Id == resena.Id);
                }

                return resena?.Reserva?.Mesa?.Restaurante?.UsuarioId == userId;
            }

            return false;
        }

        private bool ResenaExists(int id)
        {
            return _context.Resenas.Any(e => e.Id == id);
        }
    }
}