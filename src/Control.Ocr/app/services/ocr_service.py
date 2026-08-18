from __future__ import annotations

from collections.abc import Iterable
from typing import Any

import cv2
from paddleocr import PaddleOCR

from app.models.ocr_result import BoundingBox, OcrDocument, OcrLine


class OcrService:
    def __init__(self):
        self.ocr = PaddleOCR(
            enable_mkldnn=False
        )

    def read_document(self, image_path: str) -> OcrDocument:
        results = self.ocr.predict(image_path)
        image = cv2.imread(image_path, cv2.IMREAD_COLOR)
        image_height, image_width = image.shape[:2] if image is not None else (None, None)
        lines: list[OcrLine] = []

        for result in results:
            data = result.json.get("res", {})
            texts = data.get("rec_texts", [])
            scores = data.get("rec_scores", [])
            boxes = data.get("rec_boxes", [])
            polygons = data.get("rec_polys", [])

            for index, text in enumerate(texts):
                polygon = self._polygon_at(polygons, index)
                box = self._box_at(boxes, index, polygon)
                score = scores[index] if index < len(scores) else None
                lines.append(
                    OcrLine(
                        text=str(text),
                        confidence=float(score) if score is not None else None,
                        box=box,
                        polygon=polygon,
                        reading_order=len(lines),
                    )
                )

        return OcrDocument(lines=lines, image_width=image_width, image_height=image_height)

    @staticmethod
    def _polygon_at(polygons: list[Any], index: int) -> list[tuple[float, float]]:
        if index >= len(polygons):
            return []
        return [(float(point[0]), float(point[1])) for point in polygons[index]]

    @staticmethod
    def _box_at(
        boxes: list[Any], index: int, polygon: Iterable[tuple[float, float]]
    ) -> BoundingBox | None:
        if index < len(boxes):
            left, top, right, bottom = boxes[index]
            return BoundingBox(left=float(left), top=float(top), right=float(right), bottom=float(bottom))

        points = list(polygon)
        if not points:
            return None
        xs, ys = zip(*points)
        return BoundingBox(left=min(xs), top=min(ys), right=max(xs), bottom=max(ys))
