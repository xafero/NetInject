using System;
using System.Collections.Generic;
#pragma warning disable 169

namespace NetInject.Test
{
    public class SuperClass
    {
        public IDictionary<SuperDelegate, SuperStruct> MyDict { get; set; }
        public ISet<SuperStruct> MySet { get; set; }
        public IList<SuperStruct> MyList { get; set; }
        public ICollection<SuperStruct> MyColl { get; set; }
        public IEnumerable<SuperStruct> MyIter { get; set; }
        public SuperDelegate MyObject { get; set; }
        public SuperDelegate[] MyArray { get; set; }
        public SuperDelegate[,] MyTwoArray { get; set; }
        public SuperDelegate[,,] MyThreeArray { get; set; }
    }

    public delegate void SuperDelegate(string name, SuperStruct super, SuperClass my,
        out SuperClass x, ref SuperClass y, out SuperWeird z, ref SuperWeird u);

    public unsafe delegate void SuperWeird(ref uint a, out uint b, SuperStruct? c, 
        ref IList<SuperClass> d, out ISet<SuperClass[]>[] e, int* i, float** f, short*** g);

    public struct SuperStruct
    {
        IDictionary<IEnumerable<SuperDelegate[]>, ISet<IList<ICollection<Tuple<SuperDelegate, SuperStruct>>>>> MyDict;
        IDictionary<IEnumerable<SuperDelegate>[], ISet<IList<ICollection<Tuple<SuperDelegate, SuperStruct>>>>[]> MyArrayDict;

        ISet<IList<ICollection<IEnumerable<SuperDelegate[]>>>> MySet;
        ISet<IList<ICollection<IEnumerable<SuperDelegate[]>[]>>[]>[] MyArraySet;

        IList<ICollection<IEnumerable<SuperDelegate[]>>> MyList;

        ICollection<IEnumerable<SuperDelegate[]>> MyColl;

        IEnumerable<SuperDelegate[]> MyIter;

        SuperDelegate MyObject;
        SuperDelegate[] MyArray;
        SuperDelegate[][] MyTwoArray;
        SuperDelegate[][][] MyThreeArray;
    }
}