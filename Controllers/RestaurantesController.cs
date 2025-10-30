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
    public class RestaurantesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RestaurantesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Restaurantes
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var query = _context.Restaurantes.AsQueryable();

            // SOLO Restaurantes ven SUS restaurantes
            // Admin ve TODOS los restaurantes (sin filtro)
            if (User.IsInRole("Restaurante"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                query = query.Where(r => r.UsuarioId == userId);
            }
            // Admin y usuarios no autenticados ven TODOS los restaurantes

            return View(await query.ToListAsync());
        }

        // GET: Restaurantes/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var restaurante = await _context.Restaurantes
                .Include(r => r.Mesas)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (restaurante == null)
            {
                return NotFound();
            }

            return View(restaurante);
        }

        // GET: Restaurantes/Create
        [Authorize(Roles = "Admin,Restaurante")] // Admin Y Restaurante pueden crear
        public IActionResult Create()
        {
            return View();
        }

        // POST: Restaurantes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Restaurante")] // Admin Y Restaurante pueden crear
        public async Task<IActionResult> Create([Bind("Id,Nombre,Direccion,Telefono")] Restaurante restaurante)
        {
            if (ModelState.IsValid)
            {
                // Asignar usuario automáticamente
                restaurante.UsuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                _context.Add(restaurante);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(restaurante);
        }

        // GET: Restaurantes/Edit/5
        [Authorize(Roles = "Admin,Restaurante")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var restaurante = await _context.Restaurantes.FindAsync(id);
            if (restaurante == null)
            {
                return NotFound();
            }

            // Verificar permisos: Restaurante solo puede editar SUS restaurantes
            if (User.IsInRole("Restaurante") && restaurante.UsuarioId != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return Forbid();
            }

            return View(restaurante);
        }

        // POST: Restaurantes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Restaurante")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Direccion,Telefono,UsuarioId")] Restaurante restaurante)
        {
            if (id != restaurante.Id)
            {
                return NotFound();
            }

            // Verificar permisos: Restaurante solo puede editar SUS restaurantes
            if (User.IsInRole("Restaurante"))
            {
                var existingRestaurante = await _context.Restaurantes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (existingRestaurante == null || existingRestaurante.UsuarioId != User.FindFirstValue(ClaimTypes.NameIdentifier))
                {
                    return Forbid();
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(restaurante);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RestauranteExists(restaurante.Id))
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
            return View(restaurante);
        }

        // GET: Restaurantes/Delete/5
        [Authorize(Roles = "Admin")] // SOLO Admin puede eliminar restaurantes
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var restaurante = await _context.Restaurantes
                .FirstOrDefaultAsync(m => m.Id == id);
            if (restaurante == null)
            {
                return NotFound();
            }

            return View(restaurante);
        }

        // POST: Restaurantes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")] // SOLO Admin puede eliminar restaurantes
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var restaurante = await _context.Restaurantes.FindAsync(id);
            if (restaurante != null)
            {
                _context.Restaurantes.Remove(restaurante);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool RestauranteExists(int id)
        {
            return _context.Restaurantes.Any(e => e.Id == id);
        }
    }
}