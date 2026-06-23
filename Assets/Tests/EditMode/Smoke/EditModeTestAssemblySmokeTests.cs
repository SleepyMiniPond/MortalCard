using NUnit.Framework;

public class EditModeTestAssemblySmokeTests
{
    [Test]
    public void EditModeTestAssembly_CanReferenceRuntimeTypes()
    {
        var commandSet = new EffectCommandSet(System.Array.Empty<IEffectCommand>());

        Assert.IsNotNull(commandSet);
        Assert.IsEmpty(commandSet.Commands);
    }
}
