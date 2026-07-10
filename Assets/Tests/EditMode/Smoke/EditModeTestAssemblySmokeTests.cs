using NUnit.Framework;
using MortalGame.GameModel;

namespace MortalGame.Tests
{

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
}
