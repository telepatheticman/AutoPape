using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoPape
{
    class CatalogManager
    {
        public List<Catalog> catalogs;
        private Thread activeThread;
        private Catalog activeCatalog;
        public CatalogManager()
        {
            catalogs = new List<Catalog>();
        }

        public void buildAll()
        {
            foreach(var catalog in catalogs)
            {
                catalog.build();
            }
        }

        public void refresh(bool shuffle)
        {
            foreach(var catalog in catalogs)
            {
                catalog.clear();
                catalog.build();
                if (shuffle) catalog.threads.Shuffle();
            }
        }

        public void add(Catalog toAdd)
        {
            //TODO: Make this check better later
            if(!catalogs.Contains(toAdd))
            {
                catalogs.Add(toAdd);
            }
        }
    }
}
