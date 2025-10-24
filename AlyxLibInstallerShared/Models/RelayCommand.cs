//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Windows.Input;

//namespace AlyxLibInstallerShared.Models;
//public class RelayCommand<T>(Action<T> execute) : ICommand
//{
//    private readonly Action<T> _execute = execute;

//    public event EventHandler? CanExecuteChanged;
//    public bool CanExecute(object? parameter) => true;
//    public void Execute(object? parameter) => _execute((T)parameter!);
//}

//public class RelayCommand(Action execute) : ICommand
//{
//    private readonly Action _execute = execute;

//    public event EventHandler? CanExecuteChanged;
//    public bool CanExecute(object? parameter) => true;
//    public void Execute(object? parameter) => _execute();
//}