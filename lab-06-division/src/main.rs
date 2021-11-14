use opencv::{
    core::{
        abs, add, dft, idft, log, magnitude, merge, min_max_loc, no_array, split, Mat, Point, Rect,
        Scalar, VecN, BORDER_DEFAULT, CV_32F, CV_32SC1, CV_8UC1, CV_8UC3, CV_8UC4,
        DFT_COMPLEX_INPUT, DFT_COMPLEX_OUTPUT, DFT_REAL_OUTPUT, DFT_SCALE,
    },
    highgui,
    imgcodecs::{self, IMREAD_COLOR, IMREAD_GRAYSCALE},
    imgproc::{filter_2d, find_contours, flood_fill, sobel, threshold, watershed, THRESH_BINARY},
    prelude::*,
    types::VectorOfMat,
    Result,
};

fn show(name: &str, mat: &Mat) -> Result<()> {
    highgui::named_window(name, 0)?;
    highgui::imshow(name, &{
        let mut clone = mat.clone();
        mat.convert_to(&mut clone, CV_8UC1, 255.0, 0.0)?;
        clone
    })?;

    Ok(())
}

fn main() -> Result<()> {
    let raw_image = imgcodecs::imread("./src.png", IMREAD_COLOR)?;
    let image_file = {
        let img = imgcodecs::imread("./src.png", IMREAD_GRAYSCALE)?;
        let mut clone = img.clone();
        img.convert_to(&mut clone, CV_32F, 1.0 / 255.0, 0.0)?;
        clone
    };

    show("image file", &image_file)?;
    let image_sobel = {
        let mut kernel = Mat::zeros(3, 3, CV_32F)?.to_mat()?;
        for i in 0..kernel.rows() {
            for j in 0..kernel.cols() {
                if i == 1 && j == 1 {
                    continue;
                }
                *(kernel.at_2d_mut::<f32>(i, j)?) = 1.0;
            }
        }
        *(kernel.at_2d_mut::<f32>(1, 1)?) = -8.0;
        apply(&image_file, |from, to| {
            filter_2d(
                from,
                to,
                CV_32F,
                &kernel,
                Point::new(-1, -1),
                0.0,
                BORDER_DEFAULT,
            )
        })?
    };

    show("image_sobel", &image_sobel)?;

    let mut markers = {
        // find_contours(dist_8u, contours, RETR_EXTERNAL, CHAIN_APPROX_SIMPLE);
        let mut out = new_mat();
        threshold(&image_file, &mut out, 0.4, 1.0, THRESH_BINARY)?;
        apply(&out, |from, to| from.convert_to(to, CV_32SC1, 1.0, 0.0))?
    };
    show("markers", &markers)?;

    println!(
        "src.type = {}, dst.type =  {} (CV_32F = {}, CV_8UC3 = {})",
        raw_image.typ()?,
        markers.typ()?,
        CV_32F,
        CV_8UC3
    );
    watershed(&raw_image, &mut markers)?;

    let mut dst = Mat::zeros(markers.rows(), markers.cols(), CV_8UC4)?.to_mat()?;

    for i in 0..markers.rows() {
        for j in 0..markers.cols() {
            let &index = markers.at_2d::<i32>(i, j)?;
            if index > 0 {
                unsafe {
                    *(dst.at_2d::<VecN<u8, 4>>(i, j)? as *const VecN<u8, 4> as usize
                        as *mut VecN<u8, 4>) = VecN::<u8, 4>::all(255);
                }
            }
        }
    }

    highgui::named_window("watershed", 0)?;
    highgui::imshow("watershed", &dst)?;

    highgui::wait_key(-1)?;

    Ok(())
}

fn apply<F: Fn(&Mat, &mut Mat) -> Result<()>>(src: &Mat, f: F) -> Result<Mat> {
    let mut clone = src.clone();
    f(src, &mut clone)?;
    Ok(clone)
}

fn new_mat() -> Mat {
    Mat::zeros(0, 0, CV_32F).unwrap().to_mat().unwrap()
}

fn abs_image(image: &Mat) -> Result<Mat> {
    abs(image)?.to_mat()
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
    let mut clone = image.clone();
    image.convert_to(&mut clone, CV_32F, 1.0, -min)?;

    let mut clone2 = clone.clone();
    clone.convert_to(&mut clone2, CV_32F, 1.0 / (max - min), 0.0)?;
    Ok(clone2)
}
