using System;
using System.Collections.Generic;

namespace Core;

public partial class Pessoaareainteresse
{
    public uint IdPessoa { get; set; }

    public uint IdAreaInteresse { get; set; }

    public sbyte TodosEstados { get; set; }

    public virtual Areainteresse IdAreaInteresseNavigation { get; set; } = null!;

    public virtual Pessoa IdPessoaNavigation { get; set; } = null!;

    public virtual ICollection<Estadosbrasil> EstadoEstadosBrasils { get; set; } = new List<Estadosbrasil>();
}
