using MedCenter.Data;
using MedCenter.DTOs;
using MedCenter.Models;
using MedCenter.Services.Authentication.Components;
using MedCenter.Services.TurnoSv;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedCenter.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : BaseController
    {
        private readonly AppDbContext _context;
        private readonly PasswordHashService _passwordHasher;

        public AdminController(AppDbContext context, PasswordHashService passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        // GET: Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var usuarios = await _context.personas
                .Include(p => p.Medico)
                .Include(p => p.Secretaria)
                .Include(p => p.Paciente)
                .Select(p => new
                {
                    p.id,
                    p.nombre,
                    p.email,
                    Rol = p.Medico != null ? "Medico" :
                          p.Secretaria != null ? "Secretaria" :
                          p.Paciente != null ? "Paciente" : "Sin Rol"
                })
                .ToListAsync();

            return View(usuarios);
        }

        // GET: Admin/CreateUser
        public IActionResult CreateUser()
        {
            ViewBag.Roles = Enum.GetNames(typeof(RolUsuario))
                .Where(r => r != "System")
                .ToList();
            return View();
        }

        // POST: Admin/CreateUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(RegisterDTO model, string roleKey)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Roles = Enum.GetNames(typeof(RolUsuario))
                    .Where(r => r != "System")
                    .ToList();
                return View(model);
            }

            // Verificar que el email no exista
            if (await _context.personas.AnyAsync(p => p.email == model.Email))
            {
                ModelState.AddModelError("Email", "Este email ya está registrado");
                ViewBag.Roles = Enum.GetNames(typeof(RolUsuario))
                    .Where(r => r != "System")
                    .ToList();
                return View(model);
            }

            // Validar roleKey para roles especiales
            if (model.Role == "Medico" || model.Role == "Secretaria" || model.Role == "Admin")
            {
                var roleKeyEntity = await _context.role_keys
                    .FirstOrDefaultAsync(rk => rk.Role == model.Role);

                if (roleKeyEntity == null || !_passwordHasher.VerifyPassword(roleKey, roleKeyEntity.HashedKey))
                {
                    ModelState.AddModelError("", "La clave de rol es incorrecta");
                    ViewBag.Roles = Enum.GetNames(typeof(RolUsuario))
                        .Where(r => r != "System")
                        .ToList();
                    return View(model);
                }
            }

            // Crear persona
            var persona = new Persona
            {
                nombre = model.Nombre,
                email = model.Email,
                contraseña = _passwordHasher.HashPassword(model.Password)
            };

            _context.personas.Add(persona);
            await _context.SaveChangesAsync();

            // Crear la entidad específica según el rol
            switch (model.Role)
            {
                case "Medico":
                    _context.medicos.Add(new Medico { id = persona.id });
                    break;
                case "Secretaria":
                    _context.secretarias.Add(new Secretaria { id = persona.id });
                    break;
                case "Paciente":
                    _context.pacientes.Add(new Paciente { id = persona.id });
                    break;
                case "Admin":
                    // Admin no necesita tabla específica adicional, se maneja solo con el rol
                    break;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Usuario {model.Nombre} creado exitosamente con rol {model.Role}";
            return RedirectToAction(nameof(Dashboard));
        }

        // GET: Admin/EditUser/{id}
        public async Task<IActionResult> EditUser(int id)
        {
            var persona = await _context.personas
                .Include(p => p.Medico)
                .Include(p => p.Secretaria)
                .Include(p => p.Paciente)
                .FirstOrDefaultAsync(p => p.id == id);

            if (persona == null)
                return NotFound();

            var rolActual = persona.Medico != null ? RolUsuario.Medico :
                           persona.Secretaria != null ? RolUsuario.Secretaria :
                           persona.Paciente != null ? RolUsuario.Paciente : RolUsuario.System;

            ViewBag.Roles = Enum.GetNames(typeof(RolUsuario))
                .Where(r => r != "System")
                .ToList();
            ViewBag.RolActual = rolActual.ToString();
            ViewBag.PersonaId = id;
            ViewBag.Nombre = persona.nombre;
            ViewBag.Email = persona.email;

            return View();
        }

        // POST: Admin/EditUser/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(int id, string nuevoRol, string roleKey)
        {
            var persona = await _context.personas
                .Include(p => p.Medico)
                .Include(p => p.Secretaria)
                .Include(p => p.Paciente)
                .FirstOrDefaultAsync(p => p.id == id);

            if (persona == null)
                return NotFound();

            if (!Enum.TryParse<RolUsuario>(nuevoRol, out var nuevoRolEnum))
            {
                ModelState.AddModelError("", "Rol inválido");
                return RedirectToAction(nameof(EditUser), new { id });
            }

            // Validar roleKey para roles especiales
            if (nuevoRolEnum == RolUsuario.Medico || nuevoRolEnum == RolUsuario.Secretaria || nuevoRolEnum == RolUsuario.Admin)
            {
                var roleKeyEntity = await _context.role_keys
                    .FirstOrDefaultAsync(rk => rk.Role == nuevoRol);

                if (roleKeyEntity == null || !_passwordHasher.VerifyPassword(roleKey, roleKeyEntity.HashedKey))
                {
                    TempData["ErrorMessage"] = "La clave de rol es incorrecta";
                    return RedirectToAction(nameof(EditUser), new { id });
                }
            }

            // Eliminar rol actual
            if (persona.Medico != null)
                _context.medicos.Remove(persona.Medico);
            if (persona.Secretaria != null)
                _context.secretarias.Remove(persona.Secretaria);
            if (persona.Paciente != null)
                _context.pacientes.Remove(persona.Paciente);

            // Asignar nuevo rol
            switch (nuevoRolEnum)
            {
                case RolUsuario.Medico:
                    _context.medicos.Add(new Medico { id = persona.id });
                    break;
                case RolUsuario.Secretaria:
                    _context.secretarias.Add(new Secretaria { id = persona.id });
                    break;
                case RolUsuario.Paciente:
                    _context.pacientes.Add(new Paciente { id = persona.id });
                    break;
                case RolUsuario.Admin:
                    // Admin no necesita tabla específica
                    break;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Rol de {persona.nombre} cambiado exitosamente a {nuevoRol}";
            return RedirectToAction(nameof(Dashboard));
        }

        // POST: Admin/DeleteUser/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var persona = await _context.personas
                .Include(p => p.Medico)
                .Include(p => p.Secretaria)
                .Include(p => p.Paciente)
                .FirstOrDefaultAsync(p => p.id == id);

            if (persona == null)
                return NotFound();

            // No permitir eliminar al último admin
            var esAdmin = persona.Medico == null && persona.Secretaria == null && persona.Paciente == null;
            if (esAdmin)
            {
                var adminCount = await _context.personas
                    .Where(p => p.Medico == null && p.Secretaria == null && p.Paciente == null)
                    .CountAsync();

                if (adminCount <= 1)
                {
                    TempData["ErrorMessage"] = "No se puede eliminar el último administrador del sistema";
                    return RedirectToAction(nameof(Dashboard));
                }
            }

            _context.personas.Remove(persona);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Usuario {persona.nombre} eliminado exitosamente";
            return RedirectToAction(nameof(Dashboard));
        }

        // GET: Admin/ManageRoles
        public async Task<IActionResult> ManageRoles()
        {
            var roleKeys = await _context.role_keys.ToListAsync();
            return View(roleKeys);
        }

        // POST: Admin/UpdateRoleKey
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRoleKey(int id, string newKey)
        {
            if (string.IsNullOrWhiteSpace(newKey))
            {
                TempData["ErrorMessage"] = "La nueva clave no puede estar vacía";
                return RedirectToAction(nameof(ManageRoles));
            }

            var roleKey = await _context.role_keys.FindAsync(id);
            if (roleKey == null)
                return NotFound();

            roleKey.HashedKey = _passwordHasher.HashPassword(newKey);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Clave para el rol {roleKey.Role} actualizada exitosamente";
            return RedirectToAction(nameof(ManageRoles));
        }
    }
}
