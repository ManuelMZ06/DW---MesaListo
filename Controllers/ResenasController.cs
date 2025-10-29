using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MesaListo.Data;
using MesaListo.Models;

namespace MesaListo.Controllers
{
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
            var applicationDbContext = _context.Resenas.Include(r => r.Cliente).Include(r => r.Reserva);
            return View(await applicationDbContext.ToListAsync());
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
                .FirstOrDefaultAsync(m => m.Id == id);
            if (resena == null)
            {
                return NotFound();
            }

            return View(resena);
        }

        // GET: Resenas/Create
        public IActionResult Create()
        {
            ViewData["ClienteId"] = new SelectList(_context.Users, "Id", "Id");
            ViewData["ReservaId"] = new SelectList(_context.Reservas, "Id", "ClienteId");
            return View();
        }

        // POST: Resenas/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Puntuacion,Comentario,ReservaId,ClienteId,FechaCreacion")] Resena resena)
        {
            if (ModelState.IsValid)
            {
                _context.Add(resena);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ClienteId"] = new SelectList(_context.Users, "Id", "Id", resena.ClienteId);
            ViewData["ReservaId"] = new SelectList(_context.Reservas, "Id", "ClienteId", resena.ReservaId);
            return View(resena);
        }

        // GET: Resenas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var resena = await _context.Resenas.FindAsync(id);
            if (resena == null)
            {
                return NotFound();
            }
            ViewData["ClienteId"] = new SelectList(_context.Users, "Id", "Id", resena.ClienteId);
            ViewData["ReservaId"] = new SelectList(_context.Reservas, "Id", "ClienteId", resena.ReservaId);
            return View(resena);
        }

        // POST: Resenas/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Puntuacion,Comentario,ReservaId,ClienteId,FechaCreacion")] Resena resena)
        {
            if (id != resena.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
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
            ViewData["ClienteId"] = new SelectList(_context.Users, "Id", "Id", resena.ClienteId);
            ViewData["ReservaId"] = new SelectList(_context.Reservas, "Id", "ClienteId", resena.ReservaId);
            return View(resena);
        }

        // GET: Resenas/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var resena = await _context.Resenas
                .Include(r => r.Cliente)
                .Include(r => r.Reserva)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (resena == null)
            {
                return NotFound();
            }

            return View(resena);
        }

        // POST: Resenas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var resena = await _context.Resenas.FindAsync(id);
            if (resena != null)
            {
                _context.Resenas.Remove(resena);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ResenaExists(int id)
        {
            return _context.Resenas.Any(e => e.Id == id);
        }
    }
}
