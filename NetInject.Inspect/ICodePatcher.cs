namespace NetInject.Inspect
{
    internal interface ICodePatcher
    {
        void Patch(IType type);
    }
}