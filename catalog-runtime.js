(async function loadLocalCatalog() {
  try {
    const response = await fetch(new URL("catalog.json?v=same-origin-20261030", document.baseURI), { cache: "no-store" });
    if (!response.ok) return;
    const data = await response.json();
    if (!Array.isArray(data.products) || data.products.length < 100) return;

    const assetPath = file => {
      const value = String(file || "").trim();
      if (!value) return "";
      const relative = value.startsWith("assets/") ? value : "assets/" + value.replace(/^\/+/, "");
      return new URL(relative, document.baseURI).href;
    };
    const rawAssetPath = file => {
      const value = String(file || "").trim();
      if (!value) return "";
      const relative = value.startsWith("assets/") ? value : "assets/" + value.replace(/^\/+/, "");
      return "https://raw.githubusercontent.com/rileex/ts/gh-pages/" + relative;
    };
    const protectImage = (image, file) => {
      if (!image || !file) return;
      image.onerror = () => {
        const fallback = rawAssetPath(file);
        if (fallback && image.src !== fallback) image.src = fallback;
      };
    };

    products.length = 0;
    categories.length = 0;
    productPhotoMap.length = 0;
    localPhotos.length = 0;

    data.products.forEach(item => {
      products.push([item.name, item.price]);
      categories.push(item.category || "hair");
      const localPhoto = String(item.photo || "").replace(/^assets\//, "");
      productPhotoMap.push(localPhoto);
      localPhotos.push(localPhoto);
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
        image.src = assetPath(item.photo);
        protectImage(image, item.photo);
        image.alt = item.name + " — SKOK HAIR PRO";
      }
      if (description && item.description) description.textContent = item.description;
      if (meta) meta.textContent = "Артикул SK-" + String(index + 1).padStart(3, "0") + " · " + (item.category || "Догляд") + " · SKOK HAIR PRO";
      if (thumbs && Array.isArray(item.photos)) {
        thumbs.innerHTML = item.photos.map(file => '<img src="' + assetPath(file) + '" data-raw-src="' + rawAssetPath(file) + '" alt="">').join("");
        thumbs.querySelectorAll("img").forEach(thumb => {
          const file = item.photos.find(value => assetPath(value) === thumb.src) || "";
          protectImage(thumb, file);
        });
      }
      document.querySelectorAll("#detailThumbs img").forEach(thumb => {
        thumb.addEventListener("click", () => {
          if (image) image.src = thumb.src;
        });
      });
    };

    renderFeatured();
    render(products);

    document.querySelectorAll("#products .product").forEach(card => {
      const index = Number(card.dataset.productIndex);
      const item = data.products[index];
      const image = card.querySelector("img");
      if (image && item) {
        image.src = assetPath(item.photo);
        protectImage(image, item.photo);
      }
    });
    document.querySelectorAll("#featuredTrack .featured-card img").forEach((image, index) => {
      const item = data.products[index];
      if (item) {
        image.src = assetPath(item.photo);
        protectImage(image, item.photo);
      }
    });

    syncCart();
    document.documentElement.dataset.catalogCount = String(products.length);
    document.documentElement.dataset.catalogImages = String(data.products.reduce((total, item) => total + (Array.isArray(item.photos) ? item.photos.length : 0), 0));
  } catch (error) {
    console.warn("Local catalog is not available yet", error);
  }
})();
