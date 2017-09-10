using Noaster.Api;

namespace NetInject.Purge
{
    public interface ICodeValidator
    {
        void Validate(IInterface type);

        void Validate(IStruct type);

        void Validate(IClass type);
    }
}