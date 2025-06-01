using Dalamud.Plugin.Services;
using Lumina.Excel;
using Lumina.Extensions;

namespace SalanthTweaks.Extensions;

public static class ExcelModuleExtensions
{
    public static bool TryGetRow<T>(this ExcelModule excelModule, uint rowId, out T t) where T : struct, IExcelRow<T>
    {
        return excelModule.GetSheet<T>().TryGetRow(rowId, out t);
    }
    
    public static bool TryGetRow<T>(this ExcelModule excelModule, Predicate<T> pred, out T t) where T : struct, IExcelRow<T>
    {
        return excelModule.GetSheet<T>().TryGetFirst(pred, out t);
    }
}
