// When you use the "init" keyword in a property, it appends the "modreq([System.Runtime]System.Runtime.CompilerServices.IsExternalInit)"
//   (required modifier) to the setter method's signature.
// Since this class was introduced in .NET 5, when you try to use "init" keyword (which is a C# 9 feature) in any framework older than .NET 5,
//   it'll whine about IsExternalInit class being missing.

// All you have to do is add a class named IsExternalInit to the System.Runtime.CompilerServices namespace.
// I've matched the exact signature from;
// https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Runtime/CompilerServices/IsExternalInit.cs
// You can declare it as public, sealed, partial, abstract or even record or struct.

// Feeling this is not the way to do it? Don't worry! Microsoft used this dirty trick over 100 times in their codebase;
// https://source.dot.net/#q=IsExternalInit

#if !NET5_0_OR_GREATER
#pragma warning disable IDE0130
namespace System.Runtime.CompilerServices;
#pragma warning restore IDE0130
[ComponentModel.EditorBrowsable(ComponentModel.EditorBrowsableState.Never)]
internal static class IsExternalInit;
#endif