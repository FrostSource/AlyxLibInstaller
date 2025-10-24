using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlyxLibInstallerShared.Models;
public interface ISelectable
{
    public bool IsSelected { get; set; }
}
