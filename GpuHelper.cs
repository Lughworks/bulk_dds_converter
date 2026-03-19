using System;

namespace BulkDDSConverter;

public static class GpuHelper {
    public static bool IsGpuAvailable() {
        try {
            var factory = new SharpDX.DXGI.Factory1();
            return factory.Adapters1.Length > 0;
        } catch {
            return false;
        }
    }
}