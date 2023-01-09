using Newtonsoft.Json;
using System;

namespace ETWAnalyzer.Extract.Modules
{
    /// <summary>
    /// A pdb on the symbol server is identified by name, id and age.
    /// </summary>
    public class PdbIdentifier : IPdbIdentifier, IComparable<PdbIdentifier>, IEquatable<PdbIdentifier>
    {
        /// <summary>
        /// Pdb name without path
        /// </summary>
        [JsonIgnore]
        public string Name { get => DeserializeProperties().myName; }

        /// <summary>
        /// GUID which should be new for every recompile of the target binary
        /// </summary>
        [JsonIgnore]
        public Guid Id { get => DeserializeProperties().myId; }

        /// <summary>
        /// Number of PDB recompilations since PDB was created or completely rebuilt
        /// </summary>
        [JsonIgnore]
        public int Age { get => DeserializeProperties().myAge; } 

        /// <summary>
        /// Used during de/serialization to get a more compact serialized format
        /// </summary>
        public string Pdb { get; set; }

        int myAge;
        Guid myId;
        string myName;

        static char[] SplitChar = new char[] { ' ' };

        PdbIdentifier DeserializeProperties()
        {
            if( myName == null)
            {
                string[] parts = Pdb.Split(SplitChar);
                if( parts.Length != 3)
                {
                    throw new ArgumentException($"Invalid data in IdAgeName: {Pdb}");
                }

                myId = Guid.Parse(parts[0]);
                myAge = int.Parse(parts[1]);
                myName = parts[2];
            }

            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(PdbIdentifier other)
        {
            int lret =  this.Name.CompareTo(other.Name);
            if( lret == 0)
            {
                lret = this.Age.CompareTo(other.Age);
            }
            if( lret == 0 )
            {
                lret = this.Id.CompareTo(other.Id);
            }

            return lret;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(PdbIdentifier other)
        {
           if( other == null )
           {
                return false;
           }

            return this.Name == other.Name &&
                   this.Id == other.Id &&
                   this.Age == other.Age;
        }

        /// <summary>
        /// Default ctor needed for deserialize
        /// </summary>
        public PdbIdentifier()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="id"></param>
        /// <param name="age"></param>
        public PdbIdentifier(string name, Guid id, int age)
        {
            if( name == null )
            {
                throw new ArgumentNullException(nameof(name));
            }

            Pdb = $"{id} {age} {name}";
        }
        
       
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{Id} {Age} {Name}";
        }
    }
}
