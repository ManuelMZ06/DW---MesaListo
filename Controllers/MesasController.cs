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
    [Authorize(Roles = "Admin,Restaurante")]
    public class MesasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MesasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Mesas
        public async Task<IActionResult> Index()
        {
            var query = _context.Mesas.Include(m => m.Restaurante).AsQueryable();

            // SOLO Restaurantes ven sus propias mesas
            // Admin ve TODAS las mesas (sin filtro)
            if (User.IsInRole("Restaurante"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                query = query.Where(m => m.Restaurante.UsuarioId == userId);
            }
            // Admin ve todas las mesas sin filtro

            return View(await query.ToListAsync());
        }

        // GET: Mesas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mesa = await _context.Mesas
                .Include(m => m.Restaurante)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (mesa == null)
            {
                return NotFound();
            }

            // Verificar permisos
            if (!CanAccessMesa(mesa))
            {
                return Forbid();
            }

            return View(mesa);
        }

        // GET: Mesas/Create
        public IActionResult Create()
        {
            var restaurantesQuery = _context.Restaurantes.AsQueryable();

            // Restaurantes solo pueden crear mesas en sus restaurantes
            if (User.IsInRole("Restaurante"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                restaurantesQuery = restaurantesQuery.Where(r => r.UsuarioId == userId);
            }

            ViewData["RestauranteId"] = new SelectList(restaurantesQuery, "Id", "Nombre");
            return View();
        }

        // POST: Mesas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Codigo,Capacidad,RestauranteId")] Mesa mesa)
        {
            // Verificar que el restaurante pertenece al usuario (si es Restaurante)
            if (User.IsInRole("Restaurante"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var restaurante = await _context.Restaurantes
                    .FirstOrDefaultAsync(r => r.Id == mesa.RestauranteId && r.UsuarioId == userId);

                if (restaurante == null)
                {
                    ModelState.AddModelError("RestauranteId", "No tiene permisos para crear mesas en este restaurante");
                }
            }

            if (ModelState.IsValid)
            {
                mesa.Reservas = new List<Reserva>();
                _context.Add(mesa);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            var restaurantesQuery = _context.Restaurantes.AsQueryable();
            if (User.IsInRole("Restaurante"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                restaurantesQuery = restaurantesQuery.Where(r => r.UsuarioId == userId);
            }
            ViewData["RestauranteId"] = new SelectList(restaurantesQuery, "Id", "Nombre", mesa.RestauranteId);
            return View(mesa);
        }

        // GET: Mesas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // ✅ CORREGIDO: Usar Include para cargar Restaurante
            var mesa = await _context.Mesas
                .Include(m => m.Restaurante)  // ¡IMPORTANTE!
                .FirstOrDefaultAsync(m => m.Id == id);

            if (mesa == null)
            {
                return NotFound();
            }

            // Verificar permisos
            if (!CanAccessMesa(mesa))
            {
                return Forbid();
            }

            var restaurantesQuery = _context.Restaurantes.AsQueryable();
            if (User.IsInRole("Restaurante"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                restaurantesQuery = restaurantesQuery.Where(r => r.UsuarioId == userId);
            }

            ViewData["RestauranteId"] = new SelectList(restaurantesQuery, "Id", "Nombre", mesa.RestauranteId);
            return View(mesa);
        }

        // POST: Mesas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Codigo,Capacidad,RestauranteId")] Mesa mesa)
        {
            if (id != mesa.Id)
            {
                return NotFound();
            }

            // Verificar permisos
            var existingMesa = await _context.Mesas
                .Include(m => m.Restaurante)
                .AsNoTracking() // ✅ AGREGAR Esto para evitar conflicto de tracking
                .FirstOrDefaultAsync(m => m.Id == id);

            if (existingMesa == null || !CanAccessMesa(existingMesa))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // ✅ SOLUCIÓN: Usar enfoque más seguro para actualizar
                    var mesaToUpdate = await _context.Mesas.FindAsync(id);
                    if (mesaToUpdate == null)
                    {
                        return NotFound();
                    }

                    // Actualizar solo las propiedades necesarias
                    mesaToUpdate.Codigo = mesa.Codigo;
                    mesaToUpdate.Capacidad = mesa.Capacidad;
                    mesaToUpdate.RestauranteId = mesa.RestauranteId;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MesaExists(mesa.Id))
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
            var restaurantesQuery = _context.Restaurantes.AsQueryable();
            if (User.IsInRole("Restaurante"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                restaurantesQuery = restaurantesQuery.Where(r => r.UsuarioId == userId);
            }
            ViewData["RestauranteId"] = new SelectList(restaurantesQuery, "Id", "Nombre", mesa.RestauranteId);
            return View(mesa);
        }

        // GET: Mesas/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mesa = await _context.Mesas
                .Include(m => m.Restaurante)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (mesa == null)
            {
                return NotFound();
            }

            // Verificar permisos
            if (!CanAccessMesa(mesa))
            {
                return Forbid();
            }

            return View(mesa);
        }

        // POST: Mesas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var mesa = await _context.Mesas
                .Include(m => m.Restaurante)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (mesa != null)
            {
                // Verificar permisos
                if (!CanAccessMesa(mesa))
                {
                    return Forbid();
                }

                _context.Mesas.Remove(mesa);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Helper method para verificar permisos de mesa
        private bool CanAccessMesa(Mesa mesa)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (User.IsInRole("Admin")) return true;
            if (User.IsInRole("Restaurante"))
            {
                return mesa.Restaurante?.UsuarioId == userId;
            }

            return false;
        }

        private bool MesaExists(int id)
        {
            return _context.Mesas.Any(e => e.Id == id);
        }
    }
}