using MedCenter.Data;
using Microsoft.EntityFrameworkCore;

namespace MedCenter.Services.AdminService;

public class AdminService
{
    private readonly AppDbContext _context;
    public AdminService(AppDbContext appDbContext)
    {
        _context = appDbContext;
    }
    public async Task<bool> EsAdmin(int id)
    {
        int [] permitsIds = await _context.personaPermisos.Where(pp => pp.PersonaId == id).Select(pp => pp.PermisoId).ToArrayAsync();
        var adminIds = await _context.rolPermisos.Where(rp => rp.RolNombre == TurnoSv.RolUsuario.Admin).Select(rp => rp.PermisoId).ToListAsync();

        return permitsIds.Any(id => adminIds.Contains(id));
    }
}