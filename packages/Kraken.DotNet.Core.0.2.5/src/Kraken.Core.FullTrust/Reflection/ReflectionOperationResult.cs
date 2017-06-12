using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Reflection {
  public class ReflectionOperationResult {

    public ReflectionOperationResult() { }
    public ReflectionOperationResult(object target) {
      this.Target = target;
      this.TargetType = target.GetType();
    }

    public bool Success { get; set; } = false;
    public string Message { get; set; }
    public object Target { get; set; }
    public PropertyInfo TargetProperty { get; set; }
    public Type TargetType { get; set; }
  }

}
