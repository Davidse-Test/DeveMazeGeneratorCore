using DeveMazeGeneratorCore.InnerMaps.InnerStuff;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DeveMazeGeneratorCore.InnerMaps
{
    public class BitArreintjeFastInnerMapOptimizedMemoryLayout : InnerMap
    {
        private readonly unsafe long* _innerData;
        private readonly int _stride;
        private readonly GCHandle _handle;
        private readonly long[] _managedArray;

        public BitArreintjeFastInnerMapOptimizedMemoryLayout(int width, int height)
            : base(width, height)
        {
            // Calculate stride - each column needs height/64+1 longs
            _stride = (height / 64) + 1;
            
            // Allocate one contiguous array instead of jagged array
            _managedArray = new long[width * _stride];
            
            // Pin the array for unsafe access
            _handle = GCHandle.Alloc(_managedArray, GCHandleType.Pinned);
            
            unsafe
            {
                _innerData = (long*)_handle.AddrOfPinnedObject();
            }
        }

        ~BitArreintjeFastInnerMapOptimizedMemoryLayout()
        {
            if (_handle.IsAllocated)
            {
                _handle.Free();
            }
        }

        public override void FillMap(bool state)
        {
            var fillValue = state ? -1L : 0L;
            
            unsafe
            {
                var totalLongs = Width * _stride;
                for (int i = 0; i < totalLongs; i++)
                {
                    _innerData[i] = fillValue;
                }
            }
        }

        public override InnerMap Clone()
        {
            var cloned = new BitArreintjeFastInnerMapOptimizedMemoryLayout(Width, Height);
            
            unsafe
            {
                var totalBytes = Width * _stride * sizeof(long);
                Buffer.MemoryCopy(_innerData, cloned._innerData, totalBytes, totalBytes);
            }
            
            return cloned;
        }

        public override bool this[int x, int y]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                unsafe
                {
                    var columnOffset = x * _stride;
                    var longIndex = y / 64;
                    var bitIndex = y & 63; // y % 64
                    
                    return (_innerData[columnOffset + longIndex] & (1L << bitIndex)) != 0;
                }
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                unsafe
                {
                    var columnOffset = x * _stride;
                    var longIndex = y / 64;
                    var bitIndex = y & 63; // y % 64
                    var dataPtr = &_innerData[columnOffset + longIndex];
                    
                    if (value)
                    {
                        *dataPtr |= (1L << bitIndex);
                    }
                    else
                    {
                        *dataPtr &= ~(1L << bitIndex);
                    }
                }
            }
        }
    }
}