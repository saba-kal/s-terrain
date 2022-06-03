using Unity.Collections;

namespace STerrain.EndlessTerrain
{
    public class VoxelData
    {
        private float[] _values;
        public readonly int Width;
        public readonly int Height;
        public readonly int Depth;
        public int ArraySize => _values.Length;

        public VoxelData(float[] values, int size)
        {
            Width = size;
            Height = size;
            Depth = size;
            _values = values;
        }

        public VoxelData(float[] values, int width, int height, int depth)
        {
            Width = width;
            Height = height;
            Depth = depth;
            _values = values;
        }

        public VoxelData(int width, int height, int depth)
        {
            Width = width;
            Height = height;
            Depth = depth;
            _values = new float[width * height * depth];
        }

        public float this[int x, int y, int z]
        {
            get { return _values[(z * Width * Height) + (y * Width) + x]; }
            set { _values[(z * Width * Height) + (y * Width) + x] = value; }
        }

        public float[] ToArray()
        {
            return _values;
        }

        public NativeArray<float> ToNativeArray()
        {
            return new NativeArray<float>(_values, Allocator.TempJob);
        }

        public void AppendToNativeArray(NativeArray<float> voxelsArray, int index)
        {
            var voxelDataSlice = new NativeSlice<float>(voxelsArray, index * ArraySize, ArraySize);
            voxelDataSlice.CopyFrom(_values);
        }

        public static VoxelData ExtractFromNativeArray(NativeArray<float> nativeArray, int index, int size)
        {
            var flattenedArraySize = size * size * size;
            var voxelDataSlice = new NativeSlice<float>(nativeArray, index * flattenedArraySize, flattenedArraySize);
            return new VoxelData(voxelDataSlice.ToArray(), size);
        }

    }
}