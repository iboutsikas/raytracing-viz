using System.Collections;
using System.Collections.Generic;
using System.Linq;

public static class ListExtensions {
    public static void Resize<T>(this List<T> list, int size, T element = default(T)) {
        int currentCount = list.Count;

        if (currentCount > size) {
            list.RemoveRange(size, currentCount - size);
        } else {
            if (size > list.Capacity)
                list.Capacity = size;
            
            list.AddRange(Enumerable.Repeat(element, size - currentCount));
        }
    }
}
