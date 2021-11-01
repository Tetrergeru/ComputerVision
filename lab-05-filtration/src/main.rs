use opencv::{
    core::{
        dft, idft, log, magnitude, merge, min_max_loc, no_array, split, Mat, Point, Rect, Scalar,
        CV_32F, CV_8UC1, DFT_COMPLEX_INPUT, DFT_COMPLEX_OUTPUT, DFT_REAL_OUTPUT, DFT_SCALE,
    },
    highgui,
    imgcodecs::{self, IMREAD_GRAYSCALE},
    imgproc::flood_fill,
    prelude::*,
    types::VectorOfMat,
    Result,
};

mod filters;
use filters::{apply_filter, butterworth_filter, gaussian_filter, perfect_filter, rev};

fn show(name: &str, mat: &Mat) -> Result<()> {
    highgui::named_window(name, 0)?;
    highgui::imshow(name, &{
        let mut clone = mat.clone();
        mat.convert_to(&mut clone, CV_8UC1, 255.0, 0.0)?;
        clone
    })?;

    Ok(())
}

fn show_filter(name: &str, fft: &(Mat, Mat), filter: &Mat) -> Result<()> {
    let image_filter = filter;
    show(&format!("image {} filter", name), &image_filter)?;

    let image_filtered = ifft_complex(&apply_filter(&fft, &image_filter)?)?;
    show(&format!("image {} filtered", name), &image_filtered)?;

    // show(
    //     &format!("image {} filtered spectrum", name),
    //     &fft_magnitude(&fft_complex(&image_filtered)?)?,
    // )?;
    Ok(())
}

fn main() -> Result<()> {
    let image_file = {
        let img = imgcodecs::imread("./example.png", IMREAD_GRAYSCALE)?;
        let mut clone = img.clone();
        img.convert_to(&mut clone, CV_32F, 1.0 / 255.0, 0.0)?;
        clone
    };

    show("image file", &image_file)?;

    let fft = fft_complex(&image_file)?;

    show("image magnitude", &fft_magnitude(&fft)?)?;
    show("image magnitude_log", &fft_magnitude_log(&fft)?)?;

    show_filter("perfect", &fft, &perfect_filter(&image_file, 30)?)?;
    show_filter(
        "butterworth",
        &fft,
        &butterworth_filter(&image_file, 30, 1)?,
    )?;
    show_filter("gaussian", &fft, &gaussian_filter(&image_file, 30)?)?;

    show_filter(
        "rev perfect",
        &fft,
        &rev(&perfect_filter(&image_file, 30)?)?,
    )?;
    show_filter(
        "rev butterworth",
        &fft,
        &rev(&butterworth_filter(&image_file, 30, 1)?)?,
    )?;
    show_filter(
        "rev gaussian",
        &fft,
        &rev(&gaussian_filter(&image_file, 30)?)?,
    )?;
    highgui::wait_key(-1)?;

    Ok(())
}

fn ifft_complex(fft: &(Mat, Mat)) -> Result<Mat> {
    let vec_of_mat =
        VectorOfMat::from(vec![fft_shift(&fft.0.clone())?, fft_shift(&fft.1.clone())?]);
    let mut image_complex = new_mat();
    merge(&vec_of_mat, &mut image_complex)?;

    let mut result = new_mat();
    idft(&image_complex, &mut result, DFT_REAL_OUTPUT, 0)?;
    Ok(result)
}

fn fft_complex(image: &Mat) -> Result<(Mat, Mat)> {
    let vec_of_mat = VectorOfMat::from(vec![
        image.clone(),
        Mat::zeros(image.rows(), image.cols(), CV_32F)?.to_mat()?,
    ]);
    let mut image_complex = new_mat();
    merge(&vec_of_mat, &mut image_complex)?;

    let mut image_dft = image.clone();
    dft(
        &image_complex,
        &mut image_dft,
        DFT_COMPLEX_OUTPUT | DFT_COMPLEX_INPUT | DFT_SCALE,
        0,
    )?;

    let mut vec_of_mat = VectorOfMat::new();
    split(&image_dft, &mut vec_of_mat)?;

    Ok((
        fft_shift(&vec_of_mat.get(0)?)?,
        fft_shift(&vec_of_mat.get(1)?)?,
    ))
}

fn fft_magnitude(fft: &(Mat, Mat)) -> Result<Mat> {
    let mut image_magnitude = new_mat();
    magnitude(&fft.0, &fft.1, &mut image_magnitude)?;
    let image = correction(&image_magnitude)?;
    let image = mul_image(&image, 255.0)?;
    Ok(image)
}

fn fft_magnitude_log(fft: &(Mat, Mat)) -> Result<Mat> {
    let image_magnitude = fft_magnitude(&fft)?;
    let image = mul_add_image(&image_magnitude, 1.0, 1.0 / 255.0)?;
    let image = &log_image(&image)?;
    let image = correction(&image)?;
    Ok(image)
}

fn fft_shift(image: &Mat) -> Result<Mat> {
    let clone = image.clone();
    let cx = image.cols() / 2;
    let cy = image.rows() / 2;
    let mut q0 = Mat::roi(&clone, Rect::new(0, 0, cx, cy))?;
    let mut q1 = Mat::roi(&clone, Rect::new(cx, 0, cx, cy))?;
    let mut q2 = Mat::roi(&clone, Rect::new(0, cy, cx, cy))?;
    let mut q3 = Mat::roi(&clone, Rect::new(cx, cy, cx, cy))?;

    let mut tmp = q0.clone();
    q0.copy_to(&mut tmp)?;
    q3.copy_to(&mut q0)?;
    tmp.copy_to(&mut q3)?;

    q1.copy_to(&mut tmp)?;
    q2.copy_to(&mut q1)?;
    tmp.copy_to(&mut q2)?;

    Ok(clone)
}

fn new_mat() -> Mat {
    Mat::zeros(0, 0, CV_32F).unwrap().to_mat().unwrap()
}

fn mul_image(image: &Mat, mul: f64) -> Result<Mat> {
    let mut clone = image.clone();
    image.convert_to(&mut clone, CV_32F, mul, 0.0)?;
    Ok(clone)
}

fn mul_mat_image(image: &Mat, mul: &Mat) -> Result<Mat> {
    Ok(image.mul(mul, 1.0)?.to_mat()?)
}

fn mul_add_image(image: &Mat, mul: f64, add: f64) -> Result<Mat> {
    let mut clone = image.clone();
    image.convert_to(&mut clone, CV_32F, mul, add)?;
    Ok(clone)
}

fn log_image(image: &Mat) -> Result<Mat> {
    let mut clone = image.clone();
    log(&image, &mut clone)?;
    Ok(clone)
}

fn correction(image: &Mat) -> Result<Mat> {
    let mut min = 0.0;
    let mut max = 0.0;
    min_max_loc(
        image,
        Some(&mut min),
        Some(&mut max),
        None,
        None,
        &no_array()?,
    )?;
    println!("{}, {}", min, max);
    let mut clone = image.clone();
    image.convert_to(&mut clone, CV_32F, 1.0, -min)?;

    let mut clone2 = clone.clone();
    clone.convert_to(&mut clone2, CV_32F, 1.0 / (max - min), 0.0)?;
    Ok(clone2)
}

fn flooded_image(image: &Mat, seed: (i32, i32), color: (u8, u8, u8)) -> Result<Mat> {
    let mut clone = image.clone();
    let (w, h) = (image.cols(), image.rows());
    flood_fill(
        &mut clone,
        Point::new(seed.0, seed.1),
        Scalar::new(color.0 as f64, color.1 as f64, color.2 as f64, 255.0),
        &mut Rect::new(0, 0, w, h),
        Scalar::new(0.0, 0.0, 0.0, 0.0),
        Scalar::new(255.0, 255.0, 255.0, 255.0),
        4,
    )?;
    Ok(clone)
}
