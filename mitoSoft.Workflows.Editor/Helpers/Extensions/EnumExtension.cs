using DynamicData.Binding;

namespace mitoSoft.Workflows.Editor.Helpers.Extensions
{
    public static class EnumExtension
    {
        public static string Name(this System.Enum enumType)
        {
            return System.Enum.GetName(enumType.GetType(), enumType);
        }
    }

    public static class ObservableCollectionExtendedExtension
    {
        public static void AddIfAbsend(this ObservableCollectionExtended<int> enumType)
        {
            return;
        }
    }
}
