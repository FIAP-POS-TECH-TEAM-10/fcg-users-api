using System;
using System.Collections.Generic;
using System.Text;

namespace Fiap.Users.Application.Commands.CriarUsuario
{
    public class CriarUsuarioResponse
    {
        public Guid Id { get; set; }
        public required string Nome { get; set; }
        public required string Email { get; set; }
    }
}
