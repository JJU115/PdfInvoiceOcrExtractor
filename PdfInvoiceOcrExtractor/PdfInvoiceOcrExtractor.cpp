// PdfInvoiceOcrExtractor.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <tesseract/baseapi.h>
#include <leptonica/allheaders.h>
#include <opencv.hpp> 

int main()
{
    std::string outText;
    cv::Mat im = cv::imread("C:/Users/Justin/Documents/PDF-Invoice-Scanning/ocrTesttwo.jpg", cv::IMREAD_COLOR);
    tesseract::TessBaseAPI* api = new tesseract::TessBaseAPI();

    api->Init("C:/dev/vcpkg/vcpkg/packages/tesseract_x64-windows/share/tessdata", "eng", tesseract::OEM_LSTM_ONLY);
    api->SetPageSegMode(tesseract::PSM_AUTO);
    api->SetImage(im.data, im.cols, im.rows, 3, im.step);
    outText = std::string(api->GetUTF8Text());
    std::cout << outText;
    api->End();
    delete api;
    return 0;

}
    /*char* outText;

    tesseract::TessBaseAPI* api = new tesseract::TessBaseAPI();
    // Initialize tesseract-ocr with English, without specifying tessdata path
    if (api->Init("C:/dev/vcpkg/vcpkg/packages/tesseract_x64-windows/share/tessdata", "eng")) {
        fprintf(stderr, "Could not initialize tesseract.\n");
        exit(1);
    }

    // Open input image with leptonica library
    Pix* image = pixRead("C:/Users/Justin/Documents/PDF-Invoice-Scanning/ocrTesttwo.jpg");
    cv::Mat sub = image(cv::Rect(50, 200, 300, 100));
    tess.SetImage((uchar*)sub.data, sub.size().width, sub.size().height, sub.channels(), sub.step1());
    tess.Recognize(0);
    const char* out = tess.GetUTF8Text();
    api->SetImage(image);
    // Get OCR result
    outText = api->GetUTF8Text();
    printf("OCR output:\n%s", outText);

    // Destroy used object and release memory
    api->End();
    delete api;
    delete[] outText;
    pixDestroy(&image);

    return 0;
}
/*
#include <iostream>
#include <opencv.hpp>

int main(int argc, char** argv)
{
    if (argc != 2)
    {
        std::cout << " Usage: " << argv[0] << " ImageToLoadAndDisplay" << std::endl;
        return -1;
    }
    std::cout << argv[1];
    cv::Mat image;
    image = cv::imread(argv[1], cv::IMREAD_COLOR); // Read the file
    if (image.empty()) // Check for invalid input
    {
        std::cout << "Could not open or find the image" << std::endl;
        return -1;
    }
    cv::namedWindow("Display window", cv::WINDOW_AUTOSIZE); // Create a window for display.
    cv::imshow("Display window", image); // Show our image inside it.
    cv::waitKey(0); // Wait for a keystroke in the window
    return 0;
}*/