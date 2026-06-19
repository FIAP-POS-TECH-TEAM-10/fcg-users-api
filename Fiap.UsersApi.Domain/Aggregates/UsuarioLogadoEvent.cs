using System;
using System.Collections.Generic;
using System.Text;

namespace Fiap.Users.Domain.Aggregates;

public record UsuarioLogadoEvent(string usuario, string token, DateTime exp);
