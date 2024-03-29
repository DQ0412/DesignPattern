﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace OrganicFoodMVC.Models
{
    public class ShoppingCart
    {
        [Key]
        public int Id { get; set; }
        public string ApplicationUserId { get; set; }
        [ForeignKey("ApplicationUserId")]
        public ApplicationUser ApplicationUser { get; set; }

        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public Product Product { get; set; }

        [Required(ErrorMessage ="Hãy chọn số lượng")]
        [Range(1, 1000, ErrorMessage ="Chọn số lượng lớn hơn 0")]
        public int Count { get; set; }
        [NotMapped]
        public long Price { get; set; }
    }
}
/*< !--In the desolate village of Shadows Hollow, where the gray clouds perpetually shrouded the sky, lived two ill-fated souls named Evelyn and Edgar. Their lives were entwined by a curse that seemed to echo through the haunting whispers of the dense, fog-covered woods that surrounded their homes.

Evelyn, a frail but kind-hearted girl, and Edgar, a brooding and mysterious young man, were deeply in love. However, a dark secret tainted their union — a centuries-old curse that condemned every firstborn child in their families to a life of perpetual suffering.

Their families, blinded by generations of grief and despair, forbade the love between Evelyn and Edgar. The villagers believed that breaking the curse required the ultimate sacrifice — the death of one of the cursed lovers. Unbeknownst to the young couple, their union was seen as a desperate attempt to defy fate and break free from the relentless grip of the curse.

As the villagers fueled the flames of superstition, Evelyn and Edgar clung to their love, determined to rewrite their tragic destiny. They ventured into the forbidden woods, guided by ancient whispers that promised a solution to their affliction.

In the heart of the ominous forest, they discovered a dilapidated shrine where a grotesque figure, the embodiment of the curse itself, demanded a sacrifice. Terrified but resolute, Evelyn and Edgar faced an agonizing choice — to save their families from perpetual suffering, one of them must willingly embrace the cold embrace of death.

In an anguished farewell, Evelyn offered herself as the sacrifice, her tearful eyes locking with Edgar's in a silent exchange of love and despair. The curse, momentarily appeased, unleashed a torrential storm that mirrored the tempest within their hearts.

Edgar, left alone in the wake of the storm, carried the burden of the curse and the memory of his beloved Evelyn. Shadows Hollow, forever cloaked in sorrow, stood witness to a tragedy that transcended time, as the village's inhabitants whispered tales of the ill-fated lovers whose love was both their salvation and demise.

Evelyn's sacrifice became a somber legend, a chilling reminder that love, in the cruel tapestry of Shadows Hollow, could be as destructive as it was beautiful. The cursed village, forever haunted by the echoes of a love lost to the ages, stood as a desolate monument to the tragic consequences of defying the cruel whims of destiny.

Years passed, and the desolation that enshrouded Shadows Hollow intensified. The curse, though momentarily sated by Evelyn's sacrifice, continued to cast its ominous shadow over the village. The air was thick with grief, and the once-vibrant community dwindled into a ghostly existence.

Edgar, burdened by guilt and the weight of his lost love, became a solitary figure, wandering the twisted paths of the woods that had claimed Evelyn. Each step he took echoed with the memories of their stolen moments together and the heart-wrenching decision that tore them apart.

The villagers, gripped by fear and superstition, avoided Edgar, whispering that the very presence of the cursed lover could invoke the wrath of the malevolent force that had claimed Evelyn. Shadows Hollow became a place where time stood still, a purgatory for lost souls ensnared in the tendrils of an age-old curse.

One fateful night, as the moon cast an eerie glow over the village, an ethereal figure emerged from the misty woods. It was Evelyn, but not as the villagers remembered her. She appeared as a spectral being, a manifestation of the curse's relentless hold on her soul.

Driven by an otherworldly force, Evelyn's ghost sought out Edgar, her translucent form gliding through the darkened village. When she finally found him, their reunion was a bittersweet dance between the realms of the living and the dead. Edgar, tormented by grief and longing, was both captivated and horrified by the spectral presence of his lost love.

As the ghostly couple embraced beneath the skeletal branches of the cursed woods, a haunting lament echoed through Shadows Hollow. The curse, sensing the undying connection between Evelyn and Edgar, intensified its grip on their entwined souls. The villagers, paralyzed by fear, watched the tragic reunion unfold with a mixture of sorrow and dread.

In a final act of desperation, Edgar pleaded with the curse to release them from its malevolent grasp. The ghostly lovers, their figures intertwined like wisps of smoke, slowly dissipated into the night. The curse, momentarily appeased, loosened its grip on Shadows Hollow, leaving behind an emptiness that mirrored the hollowness in the hearts of those who remained.

The once-vibrant village, now a mere shell of its former self, became a cautionary tale whispered by the wind through the twisted trees. Shadows Hollow stood as a testament to the devastating consequences of a love that defied the cruel whims of destiny — a love that, even in death, refused to be extinguished.-->*/