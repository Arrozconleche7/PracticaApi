using Microsoft.IdentityModel.Tokens;
using System.Text;
namespace PracticaApi
{
    public static class ConfigJwt
    {
        public static readonly SymmetricSecurityKey Llave = new(Encoding.UTF8.GetBytes("MI_SUPER_CLAVE_SECRETA_PARA_JWT_QUE_TIENE_MAS_DE_32_LETRAS"));
    }
}
