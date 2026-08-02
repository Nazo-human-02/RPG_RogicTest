using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public abstract record SelectionResult<T> : ISelectionResult;


public record SelectionSuccess<T>(T Value) : SelectionResult<T>;

public record SelectionCancel<T> : SelectionResult<T>;
public record SelectionContinue<T> : SelectionResult<T>;
public record SelectionOpenMenu<T>(MenuContext MenuContext) : SelectionResult<T>;