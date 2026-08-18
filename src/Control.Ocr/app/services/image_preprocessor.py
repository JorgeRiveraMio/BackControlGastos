from __future__ import annotations

from pathlib import Path

import cv2


class ImagePreprocessor:
    """Applies only the low-risk preparation shared by screenshots and photos."""

    def prepare(self, source_path: str, destination_path: str) -> None:
        image = cv2.imread(source_path, cv2.IMREAD_COLOR)
        if image is None:
            raise ValueError("The uploaded file is not a readable image.")

        height, width = image.shape[:2]
        shortest_side = min(width, height)
        if shortest_side < 900:
            scale = min(2.0, 900 / shortest_side)
            image = cv2.resize(image, None, fx=scale, fy=scale, interpolation=cv2.INTER_CUBIC)

        if not cv2.imwrite(destination_path, image):
            raise ValueError("Could not prepare the uploaded image.")

    @staticmethod
    def prepared_suffix(source_path: str) -> str:
        suffix = Path(source_path).suffix.lower()
        return suffix if suffix in {".jpg", ".jpeg", ".png", ".webp"} else ".png"
