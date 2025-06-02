using DeveMazeGeneratorCore.InnerMaps.InnerStuff;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DeveMazeGeneratorCore.InnerMaps
{
    public class BitArreintjeFastInnerMapCacheOptimized : InnerMap
    {
        private readonly unsafe long* _innerData;
        private readonly int _blockSize;
        private readonly int _blocksPerRow;
        private readonly int _totalBlocks;
        private readonly GCHandle _handle;
        private readonly long[] _managedArray;

        public BitArreintjeFastInnerMapCacheOptimized(int width, int height)
            : base(width, height)
        {
            // Use cache-friendly block layout
            // Block size of 64 fits well in CPU cache lines
            _blockSize = 64;
            _blocksPerRow = (width + _blockSize - 1) / _blockSize;
            var blocksPerColumn = (height + _blockSize - 1) / _blockSize;
            _totalBlocks = _blocksPerRow * blocksPerColumn;
            
            // Each block stores _blockSize * _blockSize bits = _blockSize^2 / 64 longs
            var longsPerBlock = (_blockSize * _blockSize + 63) / 64;
            
            // Allocate one contiguous array with cache-friendly layout
            _managedArray = new long[_totalBlocks * longsPerBlock];
            
            // Pin the array for unsafe access
            _handle = GCHandle.Alloc(_managedArray, GCHandleType.Pinned);
            
            unsafe
            {
                _innerData = (long*)_handle.AddrOfPinnedObject();
            }
        }

        ~BitArreintjeFastInnerMapCacheOptimized()
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
                var totalLongs = _managedArray.Length;
                for (int i = 0; i < totalLongs; i++)
                {
                    _innerData[i] = fillValue;
                }
            }
        }

        public override InnerMap Clone()
        {
            var cloned = new BitArreintjeFastInnerMapCacheOptimized(Width, Height);
            
            unsafe
            {
                var totalBytes = _managedArray.Length * sizeof(long);
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
                    // Calculate block coordinates
                    var blockX = x / _blockSize;
                    var blockY = y / _blockSize;
                    var blockIndex = blockY * _blocksPerRow + blockX;
                    
                    // Local coordinates within block
                    var localX = x & (_blockSize - 1); // x % _blockSize
                    var localY = y & (_blockSize - 1); // y % _blockSize
                    
                    // Bit position within block
                    var bitInBlock = localY * _blockSize + localX;
                    var longIndex = bitInBlock / 64;
                    var bitIndex = bitInBlock & 63; // bitInBlock % 64
                    
                    // Calculate offset based on cache-friendly layout
                    var longsPerBlock = (_blockSize * _blockSize + 63) / 64;
                    var offset = blockIndex * longsPerBlock + longIndex;
                    
                    return (_innerData[offset] & (1L << bitIndex)) != 0;
                }
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                unsafe
                {
                    // Calculate block coordinates
                    var blockX = x / _blockSize;
                    var blockY = y / _blockSize;
                    var blockIndex = blockY * _blocksPerRow + blockX;
                    
                    // Local coordinates within block
                    var localX = x & (_blockSize - 1); // x % _blockSize
                    var localY = y & (_blockSize - 1); // y % _blockSize
                    
                    // Bit position within block
                    var bitInBlock = localY * _blockSize + localX;
                    var longIndex = bitInBlock / 64;
                    var bitIndex = bitInBlock & 63; // bitInBlock % 64
                    
                    // Calculate offset based on cache-friendly layout
                    var longsPerBlock = (_blockSize * _blockSize + 63) / 64;
                    var offset = blockIndex * longsPerBlock + longIndex;
                    var dataPtr = &_innerData[offset];
                    
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