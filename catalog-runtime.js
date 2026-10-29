(async function loadLocalCatalog() {
  try {
    const response = await fetch("catalog.json?v=full-catalog-1", { cache: "no-store" });
    if (!response.ok) return;
    const data = await response.json();
    if (!Array.isArray(data.products) || data.products.length < 100) return;

    products.length = 0;
    categories.length = 0;
    productPhotoMap.length = 0;
    localPhotos.length = 0;

    data.products.forEach(item => {
      products.push([item.name, item.price]);
      categories.push(item.category || "hair");
      productPhotoMap.push(item.photo);
      localPhotos.push(item.photo);
    });

    const importedOpenProduct = openProduct;
    openProduct = function(index) {
      importedOpenProduct(index);
      const item = data.products[index];
      if (!item) return;
      const image = document.querySelector("#detailImage");
      const description = document.querySelector("#detailDescription");
      const meta = document.querySelector("#detailMeta");
      const thumbs = document.querySelector("#detailThumbs");
      if (image) {
        image.src = item.photo;
        image.alt = item.name + " — SKOK HAIR PRO";
      }
      if (description && item.description) description.textContent = item.description;
      if (meta) meta.textContent = "Артикул SK-" + String(index + 1).padStart(3, "0") + " · " + (item.category || "Догляд") + " · SKOK HAIR PRO";
      if (thumbs && Array.isArray(item.photos)) {
        thumbs.innerHTML = item.photos.map(file => '<img src="' + file + '" alt="">').join("");
      }
      document.querySelectorAll("#detailThumbs img").forEach(thumb => {
        thumb.addEventListener("click", () => {
          if (image) image.src = thumb.src;
        });
      });
    };

    renderFeatured();
    render(products);
    syncCart();
    document.documentElement.dataset.catalogCount = String(products.length);
  } catch (error) {
    console.warn("Local catalog is not available yet", error);
  }
})(); 
