using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using static PracticaApi.Model;

namespace PracticaApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private readonly AppDb _db;
        private readonly IPasswordHasher<Usuario> _hasher;

        public UsuariosController(AppDb db, IPasswordHasher<Usuario> hasher)
        {
            _db = db;
            _hasher = hasher;
        }

        [HttpPost("registro")]
        public async Task<IActionResult> Registro(RegistroDTO dto)
        {
            if (await _db.Usuarios.AnyAsync(u => u.Correo == dto.Correo))
            {
                return BadRequest(new { mensaje = "El correo ya está registrado." });
            }

            var nuevoUsuario = new Usuario
            {
                Nombre = dto.Nombre,
                Correo = dto.Correo
            };

            nuevoUsuario.ClaveHash = _hasher.HashPassword(nuevoUsuario, dto.Clave);

            _db.Usuarios.Add(nuevoUsuario);
            await _db.SaveChangesAsync();

            return Ok(new { mensaje = "Usuario registrado con éxito." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDTO dto)
        {
            var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Correo == dto.Correo);

            if (usuario == null) return Unauthorized(new { mensaje = "Credenciales incorrectas." });

            var resultado = _hasher.VerifyHashedPassword(usuario, usuario.ClaveHash, dto.Clave);

            if (resultado == PasswordVerificationResult.Failed)
                return Unauthorized(new { mensaje = "Credenciales incorrectas." });

            // Creación del token JWT
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.Nombre),
                new Claim(ClaimTypes.Email, usuario.Correo)
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2), // El token dura 2 horas
                signingCredentials: new SigningCredentials(ConfigJwt.Llave, SecurityAlgorithms.HmacSha256)
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new { token = tokenString });
        }

        [Authorize]
        [HttpGet("perfil")]
        public async Task<IActionResult> ObtenerPerfil()
        {
            // Extrae el ID del usuario directamente del token JWT validado
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (idClaim == null) return Unauthorized();

            var usuario = await _db.Usuarios.FindAsync(int.Parse(idClaim));
            if (usuario == null) return NotFound(new { mensaje = "Usuario no encontrado." });

            var perfil = new PerfilDTO(usuario.Nombre, usuario.Disponible, usuario.AniosExperiencia);
            return Ok(perfil);
        }

        [Authorize]
        [HttpPut("perfil")]
        public async Task<IActionResult> ActualizarPerfil(PerfilDTO dto)
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (idClaim == null) return Unauthorized();

            var usuario = await _db.Usuarios.FindAsync(int.Parse(idClaim));
            if (usuario == null) return NotFound(new { mensaje = "Usuario no encontrado." });

            // Actualiza los campos permitidos
            usuario.Nombre = dto.Nombre;
            usuario.Disponible = dto.Disponible;
            usuario.AniosExperiencia = dto.AniosExperiencia;

            await _db.SaveChangesAsync();

            return Ok(new { mensaje = "Perfil actualizado correctamente." });
        }
    }
}