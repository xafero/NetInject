﻿using System;
using System.Collections.Generic;

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

    public delegate void SuperDelegate(string name, SuperStruct super, SuperClass my);

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